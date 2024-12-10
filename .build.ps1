using namespace System.Management.Automation
using namespace System.Management.Automation.Language
using namespace Microsoft.PowerShell.Commands

<#
    .DESCRIPTION
    Assumes the following layout:

    ├── LICENSE
    ├── <this file>
    ├── Module.psd1
    ├── Classes
    │   └── *.ps1
    ├── Private
    │   └── *.ps1
    ├── Public
    │   └── *.ps1
    └── Tests
        └── *.Tests.ps1
#>

[CmdletBinding()]
param
(
    [switch]$Bootstrap,

    [version]$NewVersion,

    [ValidateSet("major", "minor", "patch")]
    [string]$Release,

    [string]$PSGalleryApiKey = $env:PSGalleryApiKey,

    [string]$ModuleName = $(
        $FromFile = $MyInvocation.MyCommand.Name -replace '\.build\.ps1$'
        if ($FromFile) {$FromFile} else {
            $MyInvocation.MyCommand.Source | Split-Path | Split-Path -Leaf
        }
    ),

    [string]$ManifestPath = "$ModuleName.psd1",

    [string[]]$Include = ('*.ps1xml', '*.psrc', 'README*', 'LICENSE*'),

    [string[]]$PSScriptFolders = ('Classes', 'Private', 'Public'),

    [string]$OutputFolder = 'Build',

    [switch]$CI = ($env:CI -and $env:CI -ne "0")
)

$BuildScript = $MyInvocation.MyCommand.Source | Split-Path -Leaf

$BuildDependencies = (
    @{ModuleName = 'InvokeBuild'; ModuleVersion = '5.12.1'},
    @{ModuleName = 'Pester'; ModuleVersion = '5.6.1'},
    @{ModuleName = 'PSScriptAnalyzer'; ModuleVersion = '1.23.0'},
    @{ModuleName = 'Microsoft.PowerShell.PSResourceGet'; ModuleVersion = '1.0.6'}
)

$SelfUpdate = {
    $SourceRepo = "fsackur/ci"
    $SourceUri = "https://raw.githubusercontent.com/$SourceRepo/refs/heads/main/$BuildScript"
    try
    {
        Invoke-WebRequest $SourceUri -OutFile $BuildScript -ErrorAction Stop
    }
    catch
    {
        $_.ErrorDetails = "Failed to update build script: $_"
        Write-Error -ErrorRecord $_ -ErrorAction Stop
    }

    if (git diff --shortstat --ignore-all-space --ignore-blank-lines $BuildScript)
    {
        Write-Build Red "WARNING: Build script differs from the version in $SourceRepo."
        Write-Build Cyan "Use command: Invoke-Build SelfUpdate, Push"

        if ($WasCalledFromInvokeBuild)
        {
            git commit -m "update build script" $BuildScript
            assert ($?)
        }
    }
    else
    {
        git restore $BuildScript
    }
}

$InstallBuildDependencies = {
    $IsInteractive = [Environment]::UserInteractive -or [Environment]::GetCommandLineArgs().Where({$_.ToLower().StartsWith('-noni')})
    $ShouldConfirm = $IsInteractive -and -not $CI

    $BuildDependencies |
        Where-Object {-not (Import-Module -FullyQualifiedName $_ -PassThru -ErrorAction Ignore)} |
        Install-BuildDependencies -Confirm:$ShouldConfirm
}

#region Handle direct invocation (i.e. not Invoke-Build)
function Install-BuildDependencies
{
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    param
    (
        [Parameter(ValueFromPipeline)]
        [ModuleSpecification]$ModuleSpec,

        [switch]$Force
    )

    if ($MyInvocation.ExpectingInput) {[ModuleSpecification[]]$ModuleSpec = $input}

    if (-not $ModuleSpec) {return}
    if (-not ($Force -or $PSCmdlet.ShouldProcess($ModuleSpec, "Install"))) {throw "Confirmation declined."}

    # run in separate process, to avoid "assembly with same name is already loaded"
    # NB. properties differ because ModuleSpec.ToString() prints the original hashtable
    Write-Build -Color Cyan "Installing $ModuleSpec..."
    pwsh -NoProfile -NoLogo -NonInteractive -c "
        `$ProgressPreference = 'Ignore'
        $($ModuleSpec -join ', ') | ForEach-Object {
            Install-Module `$_.ModuleName -MinimumVersion `$_.ModuleVersion -Force -ea Stop *>&1
        }
    "
    if (-not $?) {exit 1}
    Write-Build -Color Cyan " ...done."
}

$WasCalledFromInvokeBuild = (Get-PSCallStack).Command -match 'Invoke-Build'

if (-not ($Bootstrap -or $WasCalledFromInvokeBuild))
{
    throw "Incorrect usage: '$($MyInvocation.Line)'. Use -Bootstrap to install the InvokeBuild module, then use Invoke-Build to run tasks."
}

if ($Bootstrap)
{
    if (-not $WasCalledFromInvokeBuild)
    {
        function Write-Build
        {
            param ([ConsoleColor]$Color, [string]$Text)
            Write-Host -ForegroundColor $Color $Text
        }

        function assert
        {
            param ([bool]$Invariant, [string]$Message)
            if (-not $Invariant) {Write-Build Red $Message; exit 1}
        }
    }

    try
    {
        . $SelfUpdate
    }
    catch
    {
        Write-Build Red $_
    }

    . $InstallBuildDependencies
    return
}
#endregion Handle direct invocation (i.e. not Invoke-Build)

task InstallBuildDependencies $InstallBuildDependencies

task SelfUpdate $SelfUpdate

task ParseManifest {
    $Script:Psd1SourcePath = Join-Path $BuildRoot "$ModuleName.psd1"

    $ManifestAst = [Parser]::ParseFile($Psd1SourcePath, [ref]$null, [ref]$null)
    $Script:ManifestContent = $ManifestAst.Extent.Text

    $Expression = $ManifestAst.EndBlock.Statements[0].PipelineElements[0].Expression
    $KvpAsts = $Expression.KeyValuePairs | Group-Object {$_.Item1.Value} -AsHashTable

    $Script:RootModule = $KvpAsts['RootModule'].Item2.PipelineElements[0].Expression.Value
    $Script:ManifestVersionAst = $KvpAsts['ModuleVersion'].Item2.PipelineElements[0].Expression
    $Script:ManifestVersion = [version]$ManifestVersionAst.Value
    $Script:Version = $ManifestVersion
    $Script:Tag = "v$ManifestVersion"

    assert($RootModule)
    assert($ManifestVersion)
}

task AppveyorMetadata ParseManifest, {
    $BuildVersion = $env:APPVEYOR_BUILD_VERSION
    assert ($BuildVersion)
    $Script:IsAppveyorTagBuild = $env:APPVEYOR_REPO_TAG -eq 'true'
    if ($IsAppveyorTagBuild)
    {
        Write-Build -Color Green "Building tag: $env:APPVEYOR_REPO_TAG_NAME"
        [version]$Version = $env:APPVEYOR_REPO_TAG_NAME -replace '^\D*' -replace '[^\.\d].*$'
        assert ($Version -eq $ManifestVersion)
        [int]$Build = $env:APPVEYOR_BUILD_NUMBER
        $BuildVersion = $Version, ++$Build -join '-'
        Update-AppveyorBuild -Version $BuildVersion
    }
}

task AppveyorAbortWhenHeadAlreadyTagged AppveyorMetadata, {
    if (-not $IsAppveyorTagBuild)
    {
        $Refs = (git for-each-ref --points-at HEAD) -replace '.* '
        $TagRefs = @($Refs) -match '^refs/tags/'
        if ($TagRefs)
        {
            "Commit $(git rev-parse HEAD) is already tagged in $TagRefs" | Write-Build -Color Yellow
            appveyor exit
        }
    }
}

task Clean {
    remove $OutputFolder
}

task UpdateVersion ParseManifest, {
    $Script:Version = if ($NewVersion)
    {
        $NewVersion
    }
    elseif ($Release -eq "major")
    {
        [version]::new(($ManifestVersion.Major + 1), 0, 0)
    }
    elseif ($Release -eq "minor")
    {
        [version]::new($ManifestVersion.Major, ($ManifestVersion.Minor + 1), 0)
    }
    elseif ($Release -eq "patch")
    {
        [version]::new($ManifestVersion.Major, $ManifestVersion.Minor, ($ManifestVersion.Build + 1))
    }
    else
    {
        $ManifestVersion
    }

    assert($Version -ge $ManifestVersion)

    $Script:Tag = "v$Version"

    if ($Version -gt $ManifestVersion)
    {
        $ManifestContent = (
            $ManifestContent.Substring(0, $ManifestVersionAst.Extent.StartOffset),
            $ManifestContent.Substring($ManifestVersionAst.Extent.EndOffset)
        ) -join "'$Version'"
        $ManifestContent > $Psd1SourcePath
    }
}

task Tag ParseManifest, {
    if (git diff -- $Psd1SourcePath)
    {
        git add $Psd1SourcePath
        assert($?)
        git commit -m $Tag
        assert($?)
    }

    $Output = git tag $Tag --no-sign *>&1 | Out-String | ForEach-Object Trim
    if (-not $?)
    {
        if ($Output -match 'already exists')
        {
            # If tag points to head, we don't care
            $Refs = (git show-ref $Tag --head) -replace ' .*'
            assert($Refs[0] -eq $Refs[1]) "Tag already exists and points to $($Refs[1] -replace '(?<=^.{7}).*')"
        }
        else
        {
            Write-Build Red $Output
            assert $false
        }
    }
}

task Push {
    $RemoteBranch = git rev-parse --abbrev-ref --symbolic-full-name "@{upstream}"
    $Output = git fetch ($RemoteBranch -replace '/.*') *>&1
    assert $? ($Output | Out-String)

    $MergeBase = git merge-base HEAD $RemoteBranch
    $RemoteHead = git rev-parse $RemoteBranch
    assert ($RemoteHead -eq $MergeBase) "Remote branch is ahead"

    $Output = git push *>&1
    assert $? ($Output | Out-String)

    $Output = git push --tags *>&1
    assert $? ($Output | Out-String)
}

task BuildDir ParseManifest, {
    $Script:BuildDir = [IO.Path]::Combine($PSScriptRoot, $OutputFolder, $ModuleName, $Version)
    $Script:BuiltManifest = Join-Path $BuildDir "$ModuleName.psd1"
    $Script:BuiltRootModule = Join-Path $BuildDir $RootModule
    New-Item $BuildDir -ItemType Directory -Force | Out-Null
}

task Includes BuildDir, {
    Copy-Item $Include $BuildDir
}

task BuildPowershell Clean, UpdateVersion, BuildDir, Includes, {
    $Requirements = @()
    $Usings = @()

    $Psm1Content = Get-Content -Raw $RootModule
    $Psm1Header = $Psm1Content -replace '(?s)(^|\n)#region build-inlines.*'
    $Psm1Footer = $Psm1Content -replace '(?s).*#endregion build-inlines(\n|$)'

    # case-insensitive matching
    $Folders = Get-ChildItem -Directory | Where-Object {$_.Name -in $PSScriptFolders}

    $Content = $Folders | ForEach-Object {
        $Label = ($_ | Resolve-Path -Relative) -replace '^\.[\\/]'
        $Files = $_ | Get-ChildItem -File -Recurse -Filter *.ps1

        $FileContents = $Files | ForEach-Object {
            $FileAst = [Parser]::ParseFile($_, [ref]$null, [ref]$null)
            $_Content = $FileAst.Extent.Text

            $Requirements += $FileAst.ScriptRequirements.Extent.Text
            $Usings += $FileAst.UsingStatements.Extent.Text

            # find furthest offset from start
            [int]$SnipOffset = (
                $FileAst.ScriptRequirements.Extent.EndOffset,
                $FileAst.UsingStatements.Extent.EndOffset,
                $FileAst.ParamBlock.Extent.EndOffset  # will only exist to hold PSSA suppressions
            ) |
                Sort-Object |
                Select-Object -Last 1

            $_Content.Substring($SnipOffset).Trim()
        }

        "#region $Label", ($FileContents -join "`n`n"), "#endregion $Label" | Write-Output
    }

    $Requirements = $Requirements | Write-Output | ForEach-Object Trim | Sort-Object -Unique
    $Usings = $Usings | Write-Output | ForEach-Object Trim | Sort-Object -Unique

    $Psm1Content = (
        $Requirements,
        $Usings,
        $Psm1Header,
        "",
        ($Content -join "`n`n"),
        "",
        $Psm1Footer
    ) | Write-Output

    Copy-Item $Psd1SourcePath $BuildDir
    $Psm1Content.Trim() > (Join-Path $BuildDir $RootModule)
}

task Build BuildPowershell

task Lint {
    $Files = $Include, $PSScriptFolders |
        Write-Output |
        Where-Object {Test-Path $_} |
        Get-ChildItem -Recurse

    $Files |
        ForEach-Object {
            Invoke-ScriptAnalyzer -Path $_.FullName -Recurse -Settings .\.vscode\PSScriptAnalyzerSettings.psd1
        } |
        Tee-Object -Variable PSSAOutput

    if ($PSSAOutput | Where-Object Severity -ge ([int][Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic.DiagnosticSeverity]::Warning))
    {
        throw "PSSA found code violations"
    }
}

task ImportBuiltModule BuildDir, {
    Remove-Module $ModuleName -ea Ignore
    Import-Module -Global $BuiltManifest -ea Stop
}

task UnitTest ImportBuiltModule, {
    Invoke-Pester ./tests/
}

task Test Lint, UnitTest

task Package Build, {
    Get-ChildItem $OutputFolder -File -Filter *.nupkg | Remove-Item  # PSResourceGet insists on recreating nupkg

    if (-not (Get-PSResourceRepository $ModuleName -ErrorAction Ignore))
    {
        Register-PSResourceRepository $ModuleName -Uri $OutputFolder -Trusted
    }
    try
    {
        Write-Verbose "Packaging to $OutputFolder..."
        Publish-PSResource -Path $BuildDir -Repository $ModuleName
    }
    finally
    {
        Unregister-PSResourceRepository $ModuleName
    }

    $PackageName = Get-ChildItem $OutputFolder -File -Filter *.nupkg | Select-Object -ExpandProperty Name
    $Script:PackageFile = Join-Path $OutputFolder $PackageName
}

task Zip Build, {
    $ZipName = "$ModuleName.$Version.zip"

    if ($IsLinux)
    {
        Push-Location $OutputFolder -ErrorAction Stop
        try
        {
            zip -or $ZipName $ModuleName
            assert($?)
        }
        finally
        {
            Pop-Location
        }
    }
    else
    {
        throw "Not implemented"
    }

    $Script:ZipFile = Join-Path $OutputFolder $ZipName
}

task GithubRelease Tag, Push, Package, Zip, {
    $Output = gh release view $Tag *>&1 | Out-String | ForEach-Object Trim
    if ($Output -ne "release not found")
    {
        if ($?)
        {
            assert $false "A release exists already for $Tag"
        }
        else
        {
            Write-Build Red $Output
            assert $false
        }
    }
    gh release create $Tag --notes $Tag $ZipFile $PackageFile
}

task PSGallery BuildDir, {
    if (-not $PSGalleryApiKey)
    {
        if (Get-Command rbw -ErrorAction Ignore)  # TODO: sort out SecretManagement wrapper
        {
            $PSGalleryApiKey = rbw get PSGallery
        }
        else
        {
            throw 'PSGalleryApiKey is required'
        }
    }

    Get-ChildItem -File $OutputFolder -Filter *.nupkg | Remove-Item  # PSResourceGet insists on recreating nupkg
    Publish-PSResource -Path $BuildDir -DestinationPath $OutputFolder -Repository PSGallery -ApiKey $PSGalleryApiKey
}

task Publish GithubRelease, PSGallery

# Default task
task . Clean, Build, Test
