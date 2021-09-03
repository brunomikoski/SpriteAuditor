using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace BrunoMikoski.SpriteAuditor
{
    public static class AtlasCacheUtility
    {
        public enum SpriteReferenceType
        {
            None,
            SpriteReference,
            SingleTextureType,
            MultipleTextureType,
            DefaultAssetReference
        }
        
        private static Dictionary<SpriteAtlas, Sprite[]> atlasToAllSprites = new Dictionary<SpriteAtlas, Sprite[]>();
        private static Dictionary<string, SpriteAtlas> tagToSpriteAtlas = new Dictionary<string, SpriteAtlas>();

        public static void CacheKnowAtlases()
        {
            atlasToAllSprites.Clear();

            if (EditorSettings.spritePackerMode == SpritePackerMode.BuildTimeOnly ||
                EditorSettings.spritePackerMode == SpritePackerMode.AlwaysOn)
            {
                CacheLegacySpriteAtlases();
            }
            else
            {
                CacheSpriteAtlases();
            }
            
            SpriteAuditorUtility.ClearAtlasCacheDirty();
        }

        private static void CacheSpriteAtlases()
        {
            string[] atlasGUIDs = AssetDatabase.FindAssets("t:SpriteAtlas");

            for (int i = 0; i < atlasGUIDs.Length; i++)
            {
                SpriteAtlas atlas =
                    AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(atlasGUIDs[i]));

                if (!atlas.IsIncludedInBuild())
                    continue;

                Sprite[] sprites = atlas.GetAllSprites().Distinct().ToArray();

                if (atlas.isVariant)
                {
                    if (atlas.TryGetMasterAtlas(out SpriteAtlas masterAtlas))
                        sprites = masterAtlas.GetAllSprites().Distinct().ToArray();
                }

                atlasToAllSprites.Add(atlas, sprites);
            }
        }

        private static void CacheLegacySpriteAtlases()
        {
            string[] spriteGUIDs = AssetDatabase.FindAssets("t:Sprite");
            Dictionary<SpriteAtlas, List<Sprite>> tempAtlasToSprites = new Dictionary<SpriteAtlas, List<Sprite>>();

            for (int i = 0; i < spriteGUIDs.Length; i++)
            {
                string spriteGUID = spriteGUIDs[i];
                string spritePath = AssetDatabase.GUIDToAssetPath(spriteGUID);
                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(spritePath);

                if (textureImporter.textureType != TextureImporterType.Sprite)
                    continue;

                if (string.IsNullOrEmpty(textureImporter.spritePackingTag))
                    continue;

                SpriteAtlas spriteAtlas = GetOrCreateSpriteAtlasForTag(textureImporter);

                if (!tempAtlasToSprites.ContainsKey(spriteAtlas))
                    tempAtlasToSprites.Add(spriteAtlas, new List<Sprite>());

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                tempAtlasToSprites[spriteAtlas].Add(sprite);
            }

            foreach (var atlasToSprites in tempAtlasToSprites)
            {
                atlasToAllSprites.Add(atlasToSprites.Key, atlasToSprites.Value.ToArray());
            }
        }

        private static SpriteAtlas GetOrCreateSpriteAtlasForTag(TextureImporter textureImporter)
        {
            if (!tagToSpriteAtlas.TryGetValue(textureImporter.spritePackingTag, out SpriteAtlas resultAtlas))
            {
                resultAtlas = new SpriteAtlas
                {
                    name = textureImporter.spritePackingTag
                };
                tagToSpriteAtlas.Add(textureImporter.spritePackingTag, resultAtlas);
                return resultAtlas;
            }

            return resultAtlas;
        }

        public static bool TryGetAtlasesForSprite(Sprite targetSprite, out List<SpriteAtlas> atlases)
        {
            if (SpriteAuditorUtility.IsAtlasesDirty)
                CacheKnowAtlases();
            
            atlases = new List<SpriteAtlas>();
            foreach (var atlasToSprites in atlasToAllSprites)
            {
                for (int i = 0; i < atlasToSprites.Value.Length; i++)
                {
                    if (atlasToSprites.Value[i] == targetSprite)
                        atlases.Add(atlasToSprites.Key);
                }
            }

            return atlases.Count > 0;
        }

        public static bool TryGetAtlasForSprite(Sprite targetSprite, out SpriteAtlas spriteAtlas)
        {
            if (SpriteAuditorUtility.IsAtlasesDirty)
                CacheKnowAtlases();

            if (TryGetAtlasesForSprite(targetSprite, out List<SpriteAtlas> atlases))
            {
                spriteAtlas = atlases[0]; 
                return true;
            }

            spriteAtlas = null;
            return false;
        }

        public static List<SpriteAtlas> GetAllKnowAtlases()
        {
            if (SpriteAuditorUtility.IsAtlasesDirty)
                CacheKnowAtlases();
            
            return atlasToAllSprites.Keys.ToList();
        }
        
        public static Sprite[] GetAllSpritesFromAtlas(SpriteAtlas atlas)
        {
            if (SpriteAuditorUtility.IsAtlasesDirty)
                CacheKnowAtlases();

            if (atlasToAllSprites.TryGetValue(atlas, out Sprite[] sprites))
                return sprites;

            return Array.Empty<Sprite>();
        }

        private static SpriteReferenceType GetSpriteToAtlasReferenceType(Sprite targetSprite, SpriteAtlas targetAtlas)
        {
            if (SpriteAuditorUtility.IsAtlasesDirty)
                CacheKnowAtlases();

            Object[] packables = targetAtlas.GetPackables();
            for (int i = 0; i < packables.Length; i++)
            {
                Object packable = packables[i];

                if (packable is Sprite sprite)
                {
                    if (sprite == targetSprite)
                        return SpriteReferenceType.SpriteReference;
                }
                else if (packable is Texture2D texture2d)
                {
                    if (texture2d.TryLoadSprites(out Sprite[] sprites))
                    {
                        foreach (Sprite withinSprite in sprites)
                        {
                            if (withinSprite == targetSprite)
                            {
                                if (sprites.Length == 1)
                                    return SpriteReferenceType.SingleTextureType;
                                return SpriteReferenceType.MultipleTextureType;
                            }
                        }
                    }
                }
                else if (packable is DefaultAsset defaultAsset)
                {
                    List<Sprite> allSpritesFromFolder = defaultAsset.GetChildrenObjectsOfType<Sprite>();
                    for (int j = 0; j < allSpritesFromFolder.Count; j++)
                    {
                        Sprite insideFolderSprite = allSpritesFromFolder[j];
                        if (insideFolderSprite == targetSprite)
                        {
                            return SpriteReferenceType.DefaultAssetReference;
                        }
                    }
                }
            }

            return SpriteReferenceType.None;
        }

        public static bool TryRemoveSpriteFromAtlas(Sprite targetSprite, SpriteAtlas spriteAtlas)
        {
            if (SpriteAuditorUtility.IsAtlasesDirty)
                CacheKnowAtlases();

            SpriteReferenceType referenceType = GetSpriteToAtlasReferenceType(targetSprite, spriteAtlas);
            switch (referenceType)
            {
                case SpriteReferenceType.None:
                {
                    Debug.LogWarning($"No reference between {targetSprite} to {spriteAtlas} found");
                    return false;
                }
                case SpriteReferenceType.SpriteReference:
                {
                    spriteAtlas.Remove(new [] {targetSprite as Object});
                    EditorUtility.SetDirty(spriteAtlas);
                    SpriteAuditorUtility.SetAtlasCacheDirty();
                    Debug.Log($"Removed {targetSprite} from {spriteAtlas}");
                    return true;
                }
                case SpriteReferenceType.SingleTextureType:
                {
                    Object mainTexture = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(targetSprite));
                    spriteAtlas.Remove(new[] {mainTexture});
                    EditorUtility.SetDirty(spriteAtlas);
                    SpriteAuditorUtility.SetAtlasCacheDirty();
                    Debug.Log($"Removed {targetSprite} from {spriteAtlas}");
                    return true;
                }
                case SpriteReferenceType.MultipleTextureType:
                {
                    Debug.LogError($"Cannot remove {targetSprite} from {spriteAtlas} since its a Texture with Multiple Sprites");
                    return false;
                }
                case SpriteReferenceType.DefaultAssetReference:
                {
                    Debug.LogError($"Cannot remove {targetSprite} from {spriteAtlas} since uses a Folder Reference");
                    return false;
                }
            }

            return false;
        }
    }
}
