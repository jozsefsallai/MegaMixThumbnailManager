using System.IO;

using Tommy;

internal static class ConfigParser
{
    private static TomlTable Get(string configPath)
    {
        using (var reader = File.OpenText(configPath))
        {
            var table = TOML.Parse(reader);
            return table;
        }
    }

    public static TomlTable GetGlobalConfig(string gamePath)
    {
        string configPath = Path.Combine(gamePath, "config.toml");
        return Get(configPath);
    }

    public static TomlTable GetModConfig(string modPath)
    {
        string configPath = Path.Combine(modPath, "config.toml");
        return Get(configPath);
    }
}