using System;
using System.IO;
using System.Collections.Generic;

using MikuMikuLibrary.Archives;
using MikuMikuLibrary.Databases;
using MikuMikuLibrary.IO;
using MikuMikuLibrary.Sprites;
using MikuMikuLibrary.Textures;

namespace MegaMixThumbnailManager.Core.Backend
{
    class ThumbnailManager
    {
        private static readonly string FARC_NAME = "spr_sel_pvtmb.farc";
        private static readonly string BIN_NAME = "spr_sel_pvtmb.bin";
        private static readonly string SPR_DB_BIN_NAME = "mod_spr_db.bin";
        private static readonly uint SPRITE_DATABASE_ID = 4527;
        private static readonly string SPRITE_DATABASE_NAME = "SPR_SEL_PVTMB";
        private static readonly uint DATABASE_ID_OFFSET = 58027;

        private uint globalTextureIndex = 0;

        private List<Sprite> globalSprites = new List<Sprite>();
        private List<Texture> globalTextures = new List<Texture>();

        public void HandleMod(string modPath)
        {
            if (modPath == Environment.CurrentDirectory)
            {
                // Mod is the thumbnail manager itself, skip it.
                return;
            }

            string pvtmbFarcPath = Path.Combine(modPath, "rom/2d", FARC_NAME);

            if (!File.Exists(pvtmbFarcPath)) {
                // Mod doesn't change thumbnails, skip it.
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
                    HandleEntry(entry);
            }
        }

        private void HandleEntry(EntryStream entry)
        {
            using (SpriteSet spriteSet = BinaryFile.Load<SpriteSet>(entry.Source))
            {
                for (int i = 0; i < spriteSet.TextureSet.Textures.Count; ++i)
                {
                    Texture texture = spriteSet.TextureSet.Textures[i];
                    HandleTexture(texture, i, spriteSet.Sprites);
                }
            }
        }

        private void HandleTexture(Texture texture, int textureIndex, List<Sprite> sprites)
        {
            bool hasModifiedTextures = HandleSpritesUsingTexture(textureIndex, sprites);

            if (!hasModifiedTextures)
            {
                return;
            }

            texture.Name = $"MERGE_DSCOMP_{globalTextureIndex}";
            globalTextures.Add(texture);

            globalTextureIndex++;
        }

        private bool HandleSpritesUsingTexture(int textureIndex, List<Sprite> sprites)
        {
            bool modified = false;

            foreach (Sprite sprite in sprites)
            {
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

        private bool IsSpriteProcessed(string name)
        {
            return globalSprites.FindIndex(sprite => sprite.Name == name) >= 0;
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

            FarcArchive archive = new FarcArchive();
            archive.IsCompressed = true;
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

            SpriteSetInfo spriteSetInfo = new SpriteSetInfo();
            spriteSetInfo.Id = SPRITE_DATABASE_ID;
            spriteSetInfo.Name = SPRITE_DATABASE_NAME;
            spriteSetInfo.FileName = BIN_NAME;

            uint id = DATABASE_ID_OFFSET;

            ushort spriteIndex = 0;

            foreach (Sprite sprite in spriteSet.Sprites)
            {
                SpriteInfo spriteInfo = new SpriteInfo();
                spriteInfo.Id = id++;
                spriteInfo.Name = $"{SPRITE_DATABASE_NAME}_{sprite.Name}";
                spriteInfo.Index = spriteIndex++;
                spriteSetInfo.Sprites.Add(spriteInfo);
            }

            ushort textureIndex = 0;

            foreach (Texture texture in spriteSet.TextureSet.Textures)
            {
                SpriteTextureInfo textureInfo = new SpriteTextureInfo();
                textureInfo.Id = id++;
                textureInfo.Name = $"SPRTEX_SEL_PVTMB_{texture.Name}";
                textureInfo.Index = textureIndex++;
                spriteSetInfo.Textures.Add(textureInfo);
            }

            database.SpriteSets.Add(spriteSetInfo);

            return database;
        }
    }
}
