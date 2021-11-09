using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Purchasing;
using UnityEngine;
using EditorGUILayout = UnityEditor.Experimental.Networking.PlayerConnection.EditorGUILayout;
using Object = UnityEngine.Object;

namespace BrunoMikoski.SpriteAuditor
{
    public static class SpriteAuditorUtility
    {
        private static int[] AVAILABLE_SPRITE_SIZES = {32, 64, 128, 256, 512, 1024, 2048, 4096, 8192};

        private static bool isReferencesDirty = true;
        public static bool IsReferencesDirty => isReferencesDirty;

        private static bool isMemoryDataDirty = true;
        public static bool IsMemoryDataDirty => isMemoryDataDirty;
        
        private static bool isAtlasesDirty = true;
        public static bool IsAtlasesDirty => isAtlasesDirty;
        
        private static bool isSpriteDataDirty;
        public static bool IsIsSpriteDataDirty => isSpriteDataDirty;

        private static bool isSaveDataDirty;
        public static bool IsSaveDataDirty => isSaveDataDirty;


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

        private static HashSet<Object> selectedObjects = new HashSet<Object>();
        public static HashSet<Object> SelectedObjects => selectedObjects;

        public static bool HasSelectedItems => selectedObjects.Count > 0;


        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            if (AtlasCacheUtility.UsingLegacySpritePacker)
                SetAtlasCacheDirty();
        }

        public static void SetBestSizeForTexture(SpriteData spriteData)
        {
            if (TryFindSmallerSizeTexture(spriteData, out int smallerSize))
            {
                int previousSize = spriteData.TextureImporter.maxTextureSize;
                spriteData.TextureImporter.maxTextureSize = smallerSize;
                spriteData.TextureImporter.SaveAndReimport();
                spriteData.CheckForSizeFlags();
                SetResultViewDirty();
                Debug.Log($"Update {spriteData.Sprite} maxTextureSize from: {previousSize} to: {smallerSize}");
            }
        }

        private static bool TryFindSmallerSizeTexture(SpriteData spriteData, out int smallerSize)
        {
            if (!spriteData.MaximumUsageSize.HasValue)
            {
                smallerSize = -1;
                return false;
            }

            if (spriteData.TextureImporter == null)
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
            if (!spriteData.MaximumUsageSize.HasValue)
                return false;
        
            int desired = Mathf.RoundToInt(Mathf.Max(spriteData.MaximumUsageSize.Value.x, spriteData.MaximumUsageSize.Value.y));

            if (desired < AVAILABLE_SPRITE_SIZES[0])
                return false;
            
            for (int i = 0; i < AVAILABLE_SPRITE_SIZES.Length-1; i++)
            {
                int current = AVAILABLE_SPRITE_SIZES[i];
                int next = AVAILABLE_SPRITE_SIZES[i + 1];

                if (current > desired && desired <= next)
                {
                    if (spriteData.TextureImporter == null)
                        return true;

                    return spriteData.TextureImporter.maxTextureSize != current;
                }
            }
        
            return false;
        
        }


        public static void SetMemoryDataDirty()
        {
            isMemoryDataDirty = true;
        }

        public static void ClearMemoryDataDirty()
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
        
        public static void SetAtlasCacheDirty()
        {
            isAtlasesDirty = true;
        }

        public static void ClearAtlasCacheDirty()
        {
            isAtlasesDirty = false;
        }
        
        public static void SetSpriteDataDirty()
        {
            isSpriteDataDirty = true;
        }

        public static void ClearSpriteDataDirty()
        {
            isSpriteDataDirty = false;
        }
        
        public static void SetSaveDataDirty()
        {
            isSaveDataDirty = true;
        }


        public static void ClearSaveDataDirty()
        {
            isSaveDataDirty = false;
        }
        
        public static void SetAllDirty()
        {
            SetAtlasCacheDirty();
            SetSpriteDataDirty();
            SetResultViewDirty();
            SetMemoryDataDirty();
            SetSaveDataDirty();
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

        public static bool IsObjectSelected<T>(T targetObject) where T : Object
        {
            return selectedObjects.Contains(targetObject);
        }
        
        public static void ClearSelection()
        {
            selectedObjects.Clear();
        }

        public static void SetObjectSelected<T>(T targetObject, bool isObjectsSelected) where T : Object
        {
            if (isObjectsSelected)
                selectedObjects.Add(targetObject);
            else
                selectedObjects.Remove(targetObject);
        }


        public static void DrawDefaultSelectionOptions(IEnumerable<Object> objs)
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Select None", EditorStyles.miniButtonLeft))
                {
                    foreach (Object o in objs)
                    {
                        selectedObjects.Remove(o);
                    }
                }
                
                if (GUILayout.Button("Select All", EditorStyles.miniButtonRight))
                {
                    foreach (Object o in objs)
                    {
                        selectedObjects.Add(o);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
