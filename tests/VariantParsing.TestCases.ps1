using namespace System.Collections.Generic;

@(
    @{  # /usr/share/glib-2.0/schemas/org.gnome.crypto.pgp.gschema.xml:org.gnome.crypto.pgp/encrypt-to-self
        TypeString = "b"
        ManagedType = [bool]
        InputString = "false"
        Expected = $false
    },
    @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Demo/integer-16-signed
        TypeString = "y"
        ManagedType = [char]
        InputString = "byte 0x42"
        Expected = "B"
    },
    @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Demo/integer-16-signed
        TypeString = "n"
        ManagedType = [short]
        InputString = "int16 -32768"
        Expected = -32768s
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.desktop.remote-desktop.gschema.xml:org.gnome.desktop.remote-desktop.rdp/port
        TypeString = "q"
        ManagedType = [ushort]
        InputString = "uint16 3389"
        Expected = 3389us
    },
    @{  # /usr/share/glib-2.0/schemas/com.github.libpinyin.ibus-libpinyin.gschema.xml:com.github.libpinyin.ibus-libpinyin.libpinyin/double-pinyin-schema
        TypeString = "i"
        ManagedType = [int]
        InputString = "0"
        Expected = 0
    },
    @{  # /usr/share/glib-2.0/schemas/org.fedorahosted.background-logo-extension.gschema.xml:org.fedorahosted.background-logo-extension/logo-border
        TypeString = "u"
        ManagedType = [uint]
        InputString = "uint32 50"
        Expected = 50u
    },
    @{  # /usr/share/glib-2.0/schemas/com.github.libpinyin.ibus-libpinyin.gschema.xml:com.github.libpinyin.ibus-libpinyin.libpinyin/network-dictionary-start-timestamp
        TypeString = "x"
        ManagedType = [long]
        InputString = "int64 0"
        Expected = 0l
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.nautilus.gschema.xml:org.gnome.nautilus.preferences/thumbnail-limit
        TypeString = "t"
        ManagedType = [ulong]
        InputString = "uint64 50"
        Expected = 50ul
    },
    @{  # /usr/share/glib-2.0/schemas/org.fedorahosted.background-logo-extension.gschema.xml:org.fedorahosted.background-logo-extension/logo-size
        TypeString = "d"
        ManagedType = [double]
        InputString = "9.0"
        Expected = [double]9.0
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.mutter.gschema.xml:org.gnome.mutter/overlay-key
        TypeString = "s"
        ManagedType = [string]
        InputString = "'Super_L'"
        Expected = "Super_L"
    },
    @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Demo/dbus-object-path
        TypeString = "o"
        ManagedType = [string]
        # InputString = "objectpath '/ca/desrt/dconf_editor'"
        # Expected = ""
    },
    @{  #
        TypeString = "g"
        ManagedType = [string]
        # InputString = ""
        # Expected = ""
    },
    @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Demo/dbus-handle
        TypeString = "h"
        ManagedType = [int]
        # InputString = "handle 0"
        # Expected = 0
    },
    @{  # /usr/share/glib-2.0/schemas/org.freedesktop.ibus.engine.anthy.gschema.xml:org.freedesktop.ibus.engine.anthy.dict/template
        TypeString = "v"
        ManagedType = [System.Object]
        # InputString = "<('template', '', '', '', false, 300, false, true, false, 'utf-8')>"
        # Expected =
    },
    @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Demo.Conflict2/type-conflict
        TypeString = "ms"
        ManagedType = [Dconf.Maybe[string]]
        InputString = "@ms 'test'"
        Expected = [Dconf.Maybe[string]]::Some("test")
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.Evince.gschema.xml:org.gnome.Evince/document-directory
        TypeString = "ms"
        ManagedType = [Dconf.Maybe[string]]
        InputString = "@ms nothing"
        Expected = [Dconf.Maybe[string]]::None
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.Terminal.gschema.xml:org.gnome.Terminal.Legacy.Settings/headerbar
        TypeString = "mb"
        ManagedType = [Dconf.Maybe[bool]]
        InputString = "@mb nothing"
        Expected = [Dconf.Maybe[bool]]::None
    },
    @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Demo/bytestring
        TypeString = "ay"
        ManagedType = [char[]]
        InputString = "[byte 0x48, 0x65, 0x6c, 0x6c, 0x6c]"
        Expected = "Helll".ToCharArray()
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.boxes.gschema.xml:org.gnome.boxes/window-size
        TypeString = "ai"
        ManagedType = [int[]]
        InputString = "[2530, 1388]"
        Expected = (2530, 1388)
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.Maps.gschema.xml:org.gnome.Maps/last-viewed-location
        TypeString = "ad"
        ManagedType = [double[]]
        InputString = "[0.0, 0.0]"
        Expected = (0, 0)
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.software.gschema.xml:org.gnome.software/compatible-projects
        TypeString = "as"
        ManagedType = [string[]]
        InputString = "['GNOME', 'KDE', 'XFCE']"
        Expected = @('GNOME', 'KDE', 'XFCE')
    },
    @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Demo/dbus-object-path-array
        TypeString = "ao"
        ManagedType = [string[]]
        # InputString = "[objectpath '/ca/desrt/dconf_editor/menus/appmenu', '/ca/desrt/dconf_editor/window/1']"
        # Expected =
    },
    @{  # /usr/share/glib-2.0/schemas/org.freedesktop.ibus.engine.anthy.gschema.xml:org.freedesktop.ibus.engine.anthy.dict/list
        TypeString = "av"
        ManagedType = [System.Object[]]
        # InputString = "[<('embedded', '般', 'General', '', true, 0, true, true, false, 'utf-8')>, <('zipcode', '〒', 'Zip Code Conversion', '', true, -1, false, true, false, 'utf-8')>, <('symbol', '記', 'Symbol', '', true, -1, true, false, false, 'utf-8')>, <('oldchar', '旧', 'Old Character Style', '', true, -1, false, true, false, 'utf-8')>, <('era', '年', 'Era', '', true, -1, false, true, false, 'utf-8')>, <('emoji', '😊', 'Emoji', '', true, -1, false, true, false, 'utf-8')>]"
        # Expected =
    },
    @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Demo/bytestring-array
        TypeString = "aay"
        ManagedType = [char[][]]
        InputString = "[[byte 0x48, 0x65, 0x6c, 0x6c, 0x6c], [0x57, 0x6f, 0x72, 0x6c, 0x64], [0x21]]"
        Expected = @("Helll".ToCharArray(), "World".ToCharArray(), "!".ToCharArray())
    },
    @{  # /usr/share/glib-2.0/schemas/org.freedesktop.ibus.engine.typing-booster.gschema.xml:org.freedesktop.ibus.engine.typing-booster/autosettings
        TypeString = "aas"
        ManagedType = [string[][]]
        InputString = "@aas []"
        Expected = @()
    },
    @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Demo/dict-entry
        TypeString = "{ss}"
        ManagedType = [Dictionary[string,string]]
        InputString = "{'color': 'red'}"  # actual value on my system is "{'color', 'red'}", but that's probably a dconf-editor bug
        Expected = @{"color" = "red"}
    },
    # @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Demo/weird-triv
    #     TypeString = "()"
    #     ManagedType = [Tuple]
    #     InputString = "()"
    #     Expected =
    # },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.rhythmbox.gschema.xml:  gsettings list-keys org.gnome.rhythmbox.source:/org/gnome/rhythmbox/plugins/audiocd/source/
        TypeString = "(sb)"
        ManagedType = [Tuple[string,bool]]
        InputString = "('Artist', true)"
        Expected = ('Artist', $true)
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.Settings.gschema.xml:org.gnome.Settings/window-state
        TypeString = "(iib)"
        ManagedType = [Tuple[int,int,bool]]
        InputString = "(1358, 1368, false)"
        Expected = (1358, 1368, $false)
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.baobab.gschema.xml:org.gnome.baobab.ui/window-size
        TypeString = "(ii)"
        ManagedType = [Tuple[int,int]]
        InputString = "(960, 600)"
        Expected = (960, 600)
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.Evince.gschema.xml:org.gnome.Evince.Default/window-ratio
        TypeString = "(dd)"
        ManagedType = [Tuple[double,double]]
        InputString = "(2.3523489932885906, 1.6247030878859858)"
        Expected = (2.3523489932885906, 1.6247030878859858)
    },
    @{  # /usr/share/glib-2.0/schemas/org.gtk.gtk4.Settings.ColorChooser.gschema.xml:org.gtk.gtk4.Settings.ColorChooser/selected-color
        TypeString = "(bdddd)"
        ManagedType = [Tuple[bool,double,double,double,double]]
        InputString = "(true, 0.20784313976764679, 0.51764708757400513, 0.89411765336990356, 1.0)"
        Expected = ($true, 0.20784313976764679, 0.51764708757400513, 0.89411765336990356, 1.0)
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.calendar.gschema.xml:org.gnome.calendar/weather-settings
        TypeString = "(bbsmv)"
        ManagedType = [Tuple[bool,bool,string,Dconf.Maybe[System.Object]]]
        InputString = "(true, true, '', @mv nothing)"
        Expected = ($true, $true, '', [Dconf.Maybe[string]].None)
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.GWeather4.gschema.xml:org.gnome.GWeather4/default-location
        TypeString = "(ssm(dd))"
        ManagedType = [Tuple[string,string,Dconf.Maybe[Tuple[double,double]]]]
        InputString = "('', 'EGLL', @m(dd) nothing)"
        Expected = ('', 'EGLL', [Dconf.Maybe[Tuple[double,double]]].None)
    },
    @{  # /usr/share/glib-2.0/schemas/org.freedesktop.ibus.engine.anthy.gschema.xml:org.freedesktop.ibus.engine.anthy.dict/files
        TypeString = "a{sv}"
        ManagedType = [Dictionary[string,System.Object][]]
        # InputString = "{'oldchar': <['/usr/share/ibus-anthy/dicts/oldchar.t']>, 'era': <['/usr/share/ibus-anthy/dicts/era.t']>, 'zipcode': <['/usr/share/ibus-anthy/dicts/zipcode.t']>, 'symbol': <['/usr/share/ibus-anthy/dicts/symbol.t']>, 'emoji': <['/usr/share/ibus-anthy/dicts/emoji.t']>}"
        # Expected =
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.epiphany.gschema.xml:org.gnome.Epiphany/search-engine-providers
        TypeString = "aa{sv}"
        ManagedType = [Dictionary[string,System.Object][][]]
        # InputString = "[{'name': <'DuckDuckGo'>, 'url': <'https://duckduckgo.com/?q=%s&t=epiphany&kd=-1'>, 'bang': <'!ddg'>}, {'name': <'Google'>, 'url': <'https://www.google.com/search?q=%s'>, 'bang': <'!g'>}, {'name': <'Bing'>, 'url': <'https://www.bing.com/search?q=%s'>, 'bang': <'!b'>}]"
        # Expected =
    },
    @{  # /usr/share/glib-2.0/schemas/ca.desrt.dconf-editor.gschema.xml:ca.desrt.dconf-editor.Settings/relocatable-schemas-user-paths
        TypeString = "a{ss}"
        ManagedType = [Dictionary[string,string][]]
        InputString = "{'ca.desrt.dconf-editor.Demo.Relocatable': '/ca/desrt/dconf-editor/Demo/relocatable/'}"
        Expected = @(@{'ca.desrt.dconf-editor.Demo.Relocatable' = '/ca/desrt/dconf-editor/Demo/relocatable/'})
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.gnome-system-monitor.gschema.xml:org.gnome.gnome-system-monitor/cpu-colors
        TypeString = "a(us)"
        ManagedType = [Tuple[uint,string][]]
        InputString = "[(uint32 0, '#e01b24'), (1, '#ff7800'), (2, '#f6d32d'), (3, '#33d17a'), (4, '#26a269'), (5, '#62a0ea'), (6, '#1c71d8'), (7, '#613583'), (8, '#9141ac'), (9, '#c061cb'), (10, '#ffbe6f'), (11, '#f9f06b'), (12, '#8ff0a4'), (13, '#2ec27e'), (14, '#1a5fb4'), (15, '#c061cb'), (16, '#b01c7999f332'), (17, '#7999f3328ca0'), (18, '#f33279998a0c'), (19, '#7999ad88f332'), (20, '#d103f3327999'), (21, '#f1e57999f332'), (22, '#7999f332ce6a'), (23, '#f332aaee7999')]"
        Expected = @((0, '#e01b24'), (1, '#ff7800'), (2, '#f6d32d'), (3, '#33d17a'), (4, '#26a269'), (5, '#62a0ea'), (6, '#1c71d8'), (7, '#613583'), (8, '#9141ac'), (9, '#c061cb'), (10, '#ffbe6f'), (11, '#f9f06b'), (12, '#8ff0a4'), (13, '#2ec27e'), (14, '#1a5fb4'), (15, '#c061cb'), (16, '#b01c7999f332'), (17, '#7999f3328ca0'), (18, '#f33279998a0c'), (19, '#7999ad88f332'), (20, '#d103f3327999'), (21, '#f1e57999f332'), (22, '#7999f332ce6a'), (23, '#f332aaee7999'))
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.desktop.input-sources.gschema.xml:org.gnome.desktop.input-sources/sources
        TypeString = "a(ss)"
        ManagedType = [Tuple[string,string][]]
        InputString = "[('xkb', 'gb')]"
        Expected = @(('xkb', 'gb'))
    },
    @{  # /usr/share/glib-2.0/schemas/org.gnome.epiphany.gschema.xml:org.gnome.Epiphany/search-engines
        TypeString = "a(sss)"
        ManagedType = [Tuple[string,string,string][]]
        InputString = "@a(sss) [('foo', 'bar', 'baz')]"
        Expected = @(('foo', 'bar', 'baz'))
    },
    @{  # /usr/share/glib-2.0/schemas/org.gtk.gtk4.Settings.ColorChooser.gschema.xml:org.gtk.gtk4.Settings.ColorChooser/custom-colors
        TypeString = "a(dddd)"
        ManagedType = [Tuple[double,double,double,double][]]
        InputString = "[(0.75, 0.25, 0.25, 1.0)]"
        Expected = @((0.75, 0.25, 0.25, 1.0))
    },
    @{  # /usr/share/glib-2.0/schemas/org.gtk.gtk4.Settings.EmojiChooser.gschema.xml:org.gtk.gtk4.Settings.EmojiChooser/recently-used-emoji
        TypeString = "a((aussasasu)u)"
        ManagedType = [Tuple[Tuple[uint[],string,string,string[],string[],uint],uint][]]
        # InputString = "@a((aussasasu)u) []"
        # Expected =
    },
    @{  # /usr/share/glib-2.0/schemas/org.gtk.Settings.EmojiChooser.gschema.xml:org.gtk.Settings.EmojiChooser/recent-emoji
        TypeString = "a((auss)u)"
        ManagedType = [Tuple[Tuple[uint[],string,string],uint][]]
        # InputString = "@a((auss)u) []"
        # Expected =
    }
)
