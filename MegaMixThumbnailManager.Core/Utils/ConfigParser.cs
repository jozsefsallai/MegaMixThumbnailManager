using System.IO;

using Tommy;

internal static class ConfigParser
{
    public static TomlTable Get(string gamePath)
    {
        string configPath = Path.Combine(gamePath, "config.toml");
        
        using (var reader = File.OpenText(configPath))
        {
            var table = TOML.Parse(reader);
            return table;
        }
    }
}