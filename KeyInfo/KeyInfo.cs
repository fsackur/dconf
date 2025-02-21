namespace Dconf
{
    public class KeyInfo
    {
        public KeyInfo(string path, string key, string value)
        {
            Path = path;
            Key = key;
            Value = value;
        }

        public string Path { get; init; }

        public string Key { get; init; }

        public string FullName { get => $"{Path}/{Key}"; }

        public string Value { get; init; }
    }
}
