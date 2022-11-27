using System;
using System.IO;

using MegaMixThumbnailManager.Core.Backend;

namespace MegaMixThumbnailManager.Core
{
    public class Mod
    {
        [DllExport]
        public static void OnInit()
        {
            Logger.Log("Collecting PV thumbnails.");

            ThumbnailManager manager = new ThumbnailManager();

            var basePath = Path.Combine(Environment.CurrentDirectory, "..");
            var directories = ModLoader.GetMods(basePath);

            foreach (string path in directories)
            {
                manager.HandleMod(path);
            }

            Logger.Log("Building combined PV thumbnail texture set and sprite database.");
            manager.Save();
        }

        [DllExport]
        public static void OnDispose()
        {
            Logger.Log("Mega Mix Thumbnail Manager disposed successfully.");
        }
    }
}
