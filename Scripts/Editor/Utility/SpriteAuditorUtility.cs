using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.SpriteAuditor
{
    public static class SpriteAuditorUtility
    {
        private static int[] AVAILABLE_SPRITE_SIZES = {32, 64, 128, 256, 512, 1024, 2048, 4096, 8192};

        private static bool isReferencesDirty;
        public static bool IsReferencesDirty => isReferencesDirty;

        private static bool isMemoryDataDirty;
        public static bool IsMemoryDataDirty => isMemoryDataDirty;

        private static SceneAsset cachedDontDestroyOnLoadSceneAsset;

        private static string cachedSearchText;

        public static string SearchText
        {
            get => cachedSearchText;
            set
            {
                if (!string.Equals(cachedSearchText, value, StringComparison.InvariantCultureIgnoreCase))
                {
                    cachedSearchText = value;
                    SearchSplitByComma = Array.ConvertAll(SearchText.Split(','), p => p.Trim());
                    SetResultViewDirty();
                }
            }
        }
        
        public static string[] SearchSplitByComma;
        private static float spriteUsageSizeThreshold;
        public static float SpriteUsageSizeThreshold => spriteUsageSizeThreshold;

        public static SceneAsset DontDestroyOnLoadSceneAsset
        {
            get
            {
                if (cachedDontDestroyOnLoadSceneAsset == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:SceneAsset DontDestroyOnLoad");
                    if (guids.Length > 0)
                    {
                        cachedDontDestroyOnLoadSceneAsset =
                            AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    }
                }
                
                return cachedDontDestroyOnLoadSceneAsset;
            }
        }


        public static void SetBestSizeForTexture(SpriteData spriteData)
        {
            TryFindSmallerSizeTexture(spriteData, out int smallerSize);

            spriteData.TextureImporter.maxTextureSize = smallerSize;
            spriteData.TextureImporter.SaveAndReimport();
            spriteData.CheckForSizeFlags();
        }

        private static bool TryFindSmallerSizeTexture(SpriteData spriteData, out int smallerSize)
        {
            if (!spriteData.MaximumUsageSize.HasValue)
            {
                smallerSize = -1;
                return false;
            }
            
            int maxSize = Mathf.RoundToInt(Mathf.Max(spriteData.MaximumUsageSize.Value.x,
                spriteData.MaximumUsageSize.Value.y));

            for (int i = 0; i < AVAILABLE_SPRITE_SIZES.Length; i++)
            {
                int size = AVAILABLE_SPRITE_SIZES[i];
                if (size < maxSize)
                    continue;

                if (spriteData.TextureImporter.maxTextureSize == size)
                {
                    smallerSize = -1;
                    return false;
                }
                
                smallerSize = size;
                return true;
            }

            smallerSize = -1;
            return false;
        }

        public static bool CanFixSpriteData(SpriteData spriteData)
        {
            if (spriteData.TextureImporter == null)
                return false;
            
            if (spriteData.TextureImporter.spriteImportMode != SpriteImportMode.Single)
                return false;
            
            if (!TryFindSmallerSizeTexture(spriteData, out int smallerSize)) 
                return false;
            
            return spriteData.TextureImporter.maxTextureSize != smallerSize;
        }

        public static bool CanTweakMaxSize(SpriteData spriteData)
        {
            if (!spriteData.MaximumUsageSize.HasValue || spriteData.TextureImporter == null)
                return false;

            int desired = Mathf.RoundToInt(Mathf.Max(spriteData.MaximumUsageSize.Value.x, spriteData.MaximumUsageSize.Value.y));
            for (int i = 0; i < AVAILABLE_SPRITE_SIZES.Length-1; i++)
            {
                int current = AVAILABLE_SPRITE_SIZES[i];
                int next = AVAILABLE_SPRITE_SIZES[i + 1];

                if (current > desired && desired <= next)
                    return spriteData.TextureImporter.maxTextureSize != current;
            }

            return false;

        }


        public static void SetMemoryDataDirty()
        {
            isMemoryDataDirty = true;
        }

        public static void MemoryDataLoaded()
        {
            isMemoryDataDirty = false;
        }
        
        public static void SetResultViewDirty()
        {
            isReferencesDirty = true;
        }
        
        public static void SetResultViewUpdated()
        {
            isReferencesDirty = false;
        }
        
        [MenuItem("Assets/Sprite Auditor/Find Results of Selected Sprites")]
        private static void SearchReferences()
        {
            HashSet<string> searchNames = new HashSet<string>();
            
            foreach (Object o in Selection.objects)
            {
                if (o is Sprite)
                {
                    searchNames.Add(o.name);
                }

                if (o is Texture2D)
                {
                    IEnumerable<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(o))
                        .Where(o1 => o1 is Sprite).Cast<Sprite>();
                    foreach (Sprite sprite in sprites)
                    {
                        searchNames.Add(sprite.name);
                    }
                }
            }

            SearchText = string.Join(",", searchNames);
            SpriteAuditorWindow.GetWindowInstance().Focus();
        }
        
        [MenuItem("Assets/Sprite Auditor/Find Results of Selected Sprites", true)]
        private static bool ValidateSearchReferences()
        {
            if (!SpriteAuditorWindow.IsOpen())
                return false;
            
            foreach (Object o in Selection.objects)
            {
                if (o is Sprite)
                    return true;

                if (o is Texture2D)
                {
                    IEnumerable<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(o))
                        .Where(o1 => o1 is Sprite).Cast<Sprite>();
                    return sprites.Any();
                }
                
            }

            return false;
        }

        public static void SetSizeCheckThreshold(float targetSpriteUsageSizeThreshold)
        {
            spriteUsageSizeThreshold = targetSpriteUsageSizeThreshold;
        }
    }
}
