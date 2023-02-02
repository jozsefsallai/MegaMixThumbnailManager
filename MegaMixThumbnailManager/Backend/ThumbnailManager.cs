using System;
using System.IO;
using System.Collections.Generic;

using MikuMikuLibrary.Archives;
using MikuMikuLibrary.Archives.CriMw;
using MikuMikuLibrary.Databases;
using MikuMikuLibrary.IO;
using MikuMikuLibrary.Sprites;
using MikuMikuLibrary.Textures;

namespace MegaMixThumbnailManager.Backend
{
    class ThumbnailManager
    {
        private static readonly string CPK_NAME = "diva_main.cpk";
        private static readonly string FARC_NAME = "spr_sel_pvtmb.farc";
        private static readonly string BIN_NAME = "spr_sel_pvtmb.bin";
        private static readonly string SPR_DB_BIN_NAME = "mod_spr_db.bin";
        private static readonly string MOD_PV_DB_NAME = "mod_pv_db.txt";
        private static readonly string PV_DB_NAME = "pv_db.txt";
        private static readonly uint SPRITE_DATABASE_ID = 4527;
        private static readonly string SPRITE_DATABASE_NAME = "SPR_SEL_PVTMB";
        private static readonly uint DATABASE_ID_OFFSET = 58027;
        private static readonly uint DATABASE_ID_MAX_SAFE_OFFSET = 58281;
        private static readonly uint DATABASE_ID_LARGE_OFFSET = 21001100;

        private uint globalTextureIndex = 0;

        private readonly List<Sprite> globalSprites = new List<Sprite>();
        private readonly List<Texture> globalTextures = new List<Texture>();

        private readonly HashSet<string> pvIDsWithoutThumbnails = new HashSet<string>();

        public void PreloadBaseThumbnails(string gamePath)
        {
            string cpkPath = Path.Combine(gamePath, CPK_NAME);
            if (!File.Exists(cpkPath))
            {
                throw new Exception("Game data is missing.");
            }

            using (CpkArchive cpkArchive = BinaryFile.Load<CpkArchive>(cpkPath))
            using (EntryStream cpkStream = cpkArchive.Open("rom_steam/rom/2d/" + FARC_NAME, EntryStreamMode.MemoryStream))
            using (FarcArchive farc = BinaryFile.Load<FarcArchive>(cpkStream))
            using (EntryStream farcStream = farc.Open(BIN_NAME, EntryStreamMode.MemoryStream))
            using (SpriteSet spriteSet = BinaryFile.Load<SpriteSet>(farcStream))
            {
                foreach (Sprite sprite in spriteSet.Sprites)
                {
                    globalSprites.Add(sprite);
                    globalTextureIndex = Math.Max(globalTextureIndex, sprite.TextureIndex);
                }

                foreach (Texture texture in spriteSet.TextureSet.Textures)
                {
                    globalTextures.Add(texture);
                }

                globalTextureIndex++;
            }

            Logger.Log($"Loaded {globalTextures.Count} textures and {globalSprites.Count} sprites from the base game.");
        }

        public void HandleMod(string modPath)
        {
            if (modPath == Environment.CurrentDirectory)
            {
                // Mod is the thumbnail manager itself, skip it.
                return;
            }

            if (!IsModEnabled(modPath))
            {
                // Mod is not enabled, skip it.
                return;
            }

            HashSet<string> pvIDs = GetPVIDs(modPath);

            string pvtmbFarcPath = Path.Combine(modPath, "rom/2d", FARC_NAME);

            if (!File.Exists(pvtmbFarcPath)) {
                // Mod doesn't mess with thumbnails but keep track of the PV IDs it uses, as they may contain missing thumbnails.

                foreach (string id in pvIDs)
                {
                    if (!IsSpriteProcessed(id))
                    {
                        pvIDsWithoutThumbnails.Add(id);
                    }
                }

                return;
            }

            using (var stream = File.OpenRead(pvtmbFarcPath))
            using (var archive = BinaryFile.Load<FarcArchive>(stream))
            {
                if (!archive.Contains(BIN_NAME))
                {
                    return;
                }

                using (EntryStream entry = archive.Open(BIN_NAME, EntryStreamMode.MemoryStream))
                    HandleEntry(entry, pvIDs);
            }

            pvIDsWithoutThumbnails.UnionWith(pvIDs);
        }

        private void HandleEntry(EntryStream entry, HashSet<string> pvIDs)
        {
            using (SpriteSet spriteSet = BinaryFile.Load<SpriteSet>(entry.Source))
            {
                for (int i = 0; i < spriteSet.TextureSet.Textures.Count; ++i)
                {
                    Texture texture = spriteSet.TextureSet.Textures[i];
                    HandleTexture(texture, i, spriteSet.Sprites, pvIDs);
                }
            }
        }

        private void HandleTexture(Texture texture, int textureIndex, List<Sprite> sprites, HashSet<string> pvIDs)
        {
            bool hasModifiedTextures = HandleSpritesUsingTexture(textureIndex, sprites, pvIDs);

            if (!hasModifiedTextures)
            {
                return;
            }

            texture.Name = $"MERGE_DSCOMP_{globalTextureIndex}";
            globalTextures.Add(texture);

            globalTextureIndex++;
        }

        private bool HandleSpritesUsingTexture(int textureIndex, List<Sprite> sprites, HashSet<string> pvIDs)
        {
            bool modified = false;

            foreach (Sprite sprite in sprites)
            {
                // Drop the PV ID eagerly to avoid overwriting thumbnails of songs with missing thumbnails.
                pvIDs.Remove(sprite.Name);

                if (sprite.TextureIndex != textureIndex)
                {
                    // Sprite doesn't use this texture.
                    continue;
                }

                if (IsSpriteProcessed(sprite.Name))
                {
                    // We've already dealt with this ID, just skip it.
                    continue;
                }

                sprite.TextureIndex = globalTextureIndex;
                globalSprites.Add(sprite);

                modified = true;
            }

            return modified;
        }

        private HashSet<string> GetPVIDs(string modPath)
        {
            // Try getting the PV IDs from mod_pv_db.txt.
            string modpvdbPath = Path.Combine(modPath, "rom", MOD_PV_DB_NAME);
            if (File.Exists(modpvdbPath))
            {
                return GetPVIDsFromFile(modpvdbPath);
            }

            // Some mods overwrite the entire pv_db.txt database, if mod_pv_db.txt isn't present, try this.
            string pvdbPath = Path.Combine(modPath, "rom", PV_DB_NAME);
            if (File.Exists(pvdbPath))
            {
                return GetPVIDsFromFile(pvdbPath);
            }

            // Mod doesn't have a PV DB, so we can return a set with zero capacity.
            return new HashSet<string>(0);
        }

        private HashSet<string> GetPVIDsFromFile(string path)
        {
            HashSet<string> ids = new HashSet<string>();

            foreach (string line in File.ReadLines(path))
            {
                string[] components = line.Split('.');
                if (components.Length < 2 || !components[0].StartsWith("pv_"))
                {
                    continue;
                }

                string id = components[0].Replace("pv_", "");
                ids.Add(id);
            }

            return ids;
        }

        private bool IsSpriteProcessed(string name)
        {
            return globalSprites.FindIndex(sprite => sprite.Name == name) >= 0;
        }

        private bool IsModEnabled(string modPath)
        {
            try
            {
                var config = ConfigParser.GetModConfig(modPath);
                
                if (!config.HasKey("enabled"))
                {
                    return false;
                }

                return config["enabled"].AsBoolean;
            } catch (Exception)
            {
                // weird
                return false;
            }
            
        }

        public void HandleEntriesWithMissingThumbnails()
        {
            if (pvIDsWithoutThumbnails.Count == 0)
            {
                // Yay, all mods have thumbnails. Great job, everyone!
                return;
            }

            // Prepare the "no thumbnail" texture which we will use for these mods.

            using (Stream stream = new MemoryStream(Properties.Resources.no_thumbnail))
            using (FarcArchive archive = BinaryFile.Load<FarcArchive>(stream))
            using (EntryStream entryStream = archive.Open("no_thumbnail.bin", EntryStreamMode.MemoryStream))
            using (SpriteSet spriteSet = BinaryFile.Load<SpriteSet>(entryStream))
            {
                Texture texture = spriteSet.TextureSet.Textures[0];
                texture.Name = texture.Name = $"MERGE_DSCOMP_{globalTextureIndex}";
                globalTextures.Add(texture);
            }
            
            foreach (Sprite sprite in globalSprites)
            {
                if (pvIDsWithoutThumbnails.Contains(sprite.Name))
                {
                    // Some other mod had overwritten the sprite for this song! Fix that!
                    HandleSpriteWithMissingThumbnail(sprite);
                    pvIDsWithoutThumbnails.Remove(sprite.Name);
                }
            }

            foreach (string id in pvIDsWithoutThumbnails)
            {
                // These songs never had sprites at all, so let's make some for them.

                Sprite sprite = new Sprite
                {
                    Name = id,
                    ResolutionMode = ResolutionMode.HDTV1080
                };

                HandleSpriteWithMissingThumbnail(sprite);
                globalSprites.Add(sprite);
            }
        }

        private void HandleSpriteWithMissingThumbnail(Sprite sprite)
        {
            // `globalTextureIndex` will point to our "no thumbnail" texture now.

            sprite.TextureIndex = globalTextureIndex;
            sprite.X = 2;
            sprite.Y = 2;
            sprite.Width = 512;
            sprite.Height = 128;

            Logger.Log($"Song with ID {sprite.Name} does not have a thumbnail.");
        }

        public void Save()
        {
            Logger.Log($"Collected {globalSprites.Count} sprites and {globalTextures.Count} textures.");

            SpriteSet spriteSet = new SpriteSet();

            foreach (Sprite sprite in globalSprites)
            {
                spriteSet.Sprites.Add(sprite);
            }

            foreach (Texture texture in globalTextures)
            {
                spriteSet.TextureSet.Textures.Add(texture);
            }

            Stream stream = new MemoryStream();
            spriteSet.Save(stream, true);

            FarcArchive archive = new FarcArchive
            {
                IsCompressed = true
            };

            archive.Add(BIN_NAME, stream, true);

            try
            {
                string outputPath = Path.Combine(Environment.CurrentDirectory, "rom/2d", FARC_NAME);
                archive.Save(outputPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return;
            }

            SpriteDatabase database = BuildDatabase(spriteSet);

            try
            {
                string outputPath = Path.Combine(Environment.CurrentDirectory, "rom/2d", SPR_DB_BIN_NAME);
                database.Save(outputPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return;
            }

            database.Dispose();
            archive.Dispose();
            stream.Dispose();
            spriteSet.Dispose();
        }

        private SpriteDatabase BuildDatabase(SpriteSet spriteSet)
        {
            SpriteDatabase database = new SpriteDatabase();

            SpriteSetInfo spriteSetInfo = new SpriteSetInfo
            {
                Id = SPRITE_DATABASE_ID,
                Name = SPRITE_DATABASE_NAME,
                FileName = BIN_NAME
            };

            uint id = DATABASE_ID_OFFSET;

            ushort textureIndex = 0;

            foreach (Texture texture in spriteSet.TextureSet.Textures)
            {
                SpriteTextureInfo textureInfo = new SpriteTextureInfo
                {
                    Id = id,
                    Name = $"SPRTEX_SEL_PVTMB_{texture.Name}",
                    Index = textureIndex++
                };
                spriteSetInfo.Textures.Add(textureInfo);

                if (id == DATABASE_ID_MAX_SAFE_OFFSET)
                {
                    id = DATABASE_ID_LARGE_OFFSET;
                }
                else
                {
                    id++;
                }
            }

            ushort spriteIndex = 0;

            foreach (Sprite sprite in spriteSet.Sprites)
            {
                SpriteInfo spriteInfo = new SpriteInfo
                {
                    Id = id,
                    Name = $"{SPRITE_DATABASE_NAME}_{sprite.Name}",
                    Index = spriteIndex++
                };

                spriteSetInfo.Sprites.Add(spriteInfo);

                if (id == DATABASE_ID_MAX_SAFE_OFFSET)
                {
                    id = DATABASE_ID_LARGE_OFFSET;
                } else
                {
                    id++;
                }
            }

            database.SpriteSets.Add(spriteSetInfo);

            return database;
        }
    }
}
