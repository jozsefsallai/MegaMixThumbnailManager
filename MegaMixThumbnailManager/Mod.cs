using System;
using System.IO;

using MegaMixThumbnailManager.Backend;

namespace MegaMixThumbnailManager
{
    public class Mod
    {
        [DllExport]
        public static void Init()
        {
            Logger.Log("Collecting PV thumbnails.");

            ThumbnailManager manager = new ThumbnailManager();

            var basePath = Path.Combine(Environment.CurrentDirectory, "..");

            try
            {
                var gamePath = Path.Combine(basePath, "..");
                manager.PreloadBaseThumbnails(gamePath);
            } catch (Exception ex)
            {
                // Your CPK is cursed, I feel like if this error is thrown, you have bigger problems than the thumbnail manager not working.
                Logger.Error("Failed to load base game thumbnails. You're on your own.");
                Logger.Error(ex);
            }

            var directories = ModLoader.GetMods(basePath);

            foreach (string path in directories)
            {
                manager.HandleMod(Path.GetFullPath(path));
            }

            try
            {
                manager.HandleEntriesWithMissingThumbnails();
            } catch (Exception ex)
            {
                // This shouldn't technically ever happen but I don't know what cursed architecture or emulation layer you're trying to run this on.
                // (And the horse would probably be alerted if they found out I'm not doing proper error handling for something that shouldn't crash the game.)
                Logger.Error("Tried fixing missing thumbnails but ran into an unexpected error. Apologies if you see blank/white thumbnails (and blame SEGA while you're at it).");
                Logger.Error(ex);
            }

            Logger.Log("Building combined PV thumbnail texture set and sprite database.");
            manager.Save();
        }

        [DllExport]
        public static void Dispose()
        {
            Logger.Log("Mega Mix Thumbnail Manager disposed successfully.");
        }
    }
}
