using System;
using System.Collections.Generic;
using System.IO;

internal static class ModLoader
{
    public static string[] GetMods(string basePath)
    {
        try
        {
            string gamePath = Path.Combine(basePath, "..");
            var settings = ConfigParser.GetGlobalConfig(gamePath);

            if (settings.HasKey("priority"))
            {
                var priorityArray = settings["priority"].AsArray;

                List<string> mods = new List<string>();
                foreach (var item in priorityArray.RawArray)
                {
                    mods.Add(item.AsString);
                }

                return GetModsPriorityLine(basePath, mods);
            }
        } catch (Exception)
        {
            // ignore
        }

        return GetModsAlphanumeric(basePath);
    }

    private static string[] GetModsPriorityLine(string basePath, List<string> mods)
    {
        Logger.Log("Processing mods from priority line.");

        List<string> finalMods = new List<string>();

        for (int i = mods.Count - 1; i >= 0; i--)
        {
            finalMods.Add(Path.Combine(basePath, mods[i]));
        }

        return finalMods.ToArray();
    }

    private static string[] GetModsAlphanumeric(string basePath)
    {
        Logger.Log("Processing mods in reverse alphabetical order.");

        var directories = Directory.GetDirectories(basePath);
        Array.Reverse(directories);
        return directories;
    }
}