using System;
using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.SpriteAuditor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace BrunoMikoski.SpriteAuditor
{
    [Serializable]
    public class SpriteData
    {
        private const string RESOURCES_UNITY_BUILTIN_EXTRA_PATH = "Resources/unity_builtin_extra";

        [SerializeField] 
        private string spriteTextureGUID;
        [SerializeField] 
        private string spriteAtlasGUID;
        [SerializeField] 
        private string spriteName;
        [SerializeField] 
        private Vector3? maximumUsageSize = null;
        public Vector3? MaximumUsageSize => maximumUsageSize;
        [SerializeField]
        private string spriteTexturePath;
        [SerializeField] 
        private List<SpriteUseData> usages = new List<SpriteUseData>();
        public List<SpriteUseData> Usages => usages;
        [SerializeField] 
        private HashSet<string> scenesPath = new HashSet<string>();
        
        private HashSet<SceneAsset> cachedSceneAssets = new HashSet<SceneAsset>();
        public HashSet<SceneAsset> SceneAssets
        {
            get
            {
                if (cachedSceneAssets == null || cachedSceneAssets.Count != scenesPath.Count)
                {
                    cachedSceneAssets = new HashSet<SceneAsset>();
                    foreach (string scenePath in scenesPath)
                    {
                        cachedSceneAssets.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath));
                    }
                }

                return cachedSceneAssets;
            }
        }

        [SerializeField] 
        private SpriteUsageFlags spriteUsageFlags;
        public SpriteUsageFlags SpriteUsageFlags => spriteUsageFlags;

        [SerializeField] 
        private float atlasScale = 1;

        private Sprite cachedSprite;
        public Sprite Sprite
        {
            get
            {
                if (cachedSprite == null)
                {
                    if (!string.IsNullOrEmpty(spriteTextureGUID))
                    {
                        cachedSprite = AssetDatabase.LoadAllAssetsAtPath(spriteTexturePath).FirstOrDefault(o =>
                            string.Equals(o.name, spriteName, StringComparison.Ordinal)) as Sprite;

                        if (cachedSprite == null)
                            cachedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteTexturePath);
                    }
                }

                return cachedSprite;
            }
        }
        
        public Vector2 SpriteSize => Sprite.rect.size * atlasScale;

        private SpriteAtlas cachedSpriteAtlas;
        public SpriteAtlas SpriteAtlas
        {
            get
            {
                if (cachedSpriteAtlas == null)
                {
                    if (!string.IsNullOrEmpty(spriteAtlasGUID))
                    {
                        cachedSpriteAtlas =
                            AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(spriteAtlasGUID));
                    }
                }

                return cachedSpriteAtlas;
            }
        }

        private TextureImporter cachedTextureImporter;
        public TextureImporter TextureImporter
        {
            get
            {
                if (cachedTextureImporter == null)
                    cachedTextureImporter = AssetImporter.GetAtPath(spriteTexturePath) as TextureImporter;

                return cachedTextureImporter;
            }
        }

        private bool firstSizeReport = true;

        public SpriteData(Sprite targetSprite)
        {
            cachedSprite = targetSprite;
            spriteTexturePath = AssetDatabase.GetAssetPath(cachedSprite);
            spriteTextureGUID = AssetDatabase.AssetPathToGUID(spriteTexturePath);
            spriteName = targetSprite.name;

            if (string.Equals(spriteTexturePath, RESOURCES_UNITY_BUILTIN_EXTRA_PATH))
            {
                spriteUsageFlags |= SpriteUsageFlags.DefaultUnityAsset;
                return;
            }
            
            if (AtlasCacheUtility.TryGetAtlasForSprite(targetSprite, out SpriteAtlas spriteAtlas))
            {
                cachedSpriteAtlas = spriteAtlas;
                spriteAtlasGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(spriteAtlas));
                if (AtlasCacheUtility.TryGetAtlasScale(spriteAtlas, out atlasScale))
                    spriteUsageFlags |= SpriteUsageFlags.UsingScaledAtlasSize;
            }
        }
        
        public void ReportUse(GameObject instance, Vector3? size)
        {
            string usagePath = instance.transform.GetPath();
            SpriteUseData spriteUsageData = GetOrCreateSpriteUsageData(instance, usagePath);
            Scene instanceScene = instance.scene;
            ReportScene(instanceScene);
            spriteUsageData.ReportPath(usagePath, instanceScene);
            ReportSize(size);
        }

        private void ReportScene(Scene scene)
        {
            if (scene.buildIndex == -1 || string.IsNullOrEmpty(scene.path))
                spriteUsageFlags |= SpriteUsageFlags.UsedOnDontDestroyOrUnknowScene;
            else
            {
                if (scenesPath.Add(scene.path))
                    SpriteAuditorUtility.SetResultViewDirty();
            }
        }

        public void ReportUse(SpriteRenderer spriteRenderer)
        {
            Vector3? size = null;
            if (CullingMaskUtility.TryGetCameraForGameObject(spriteRenderer.gameObject, out Camera camera))
                size = spriteRenderer.GetPixelSize(camera);

            ReportUse(spriteRenderer.gameObject, size);
        }

        public void ReportUse(SpriteMask spriteMask)
        {
            Vector3? size = null;
            if (CullingMaskUtility.TryGetCameraForGameObject(spriteMask.gameObject, out Camera camera))
                size = spriteMask.GetPixelSize(camera);

            ReportUse(spriteMask.gameObject, size);
        }

        public void ReportUse(Image image)
        {
            Vector3? size = null;
            if (image.type != Image.Type.Tiled && image.type != Image.Type.Sliced && image.type != Image.Type.Filled)
                size = image.GetPixelSize();

            ReportUse(image.gameObject, size);
        }

        private void ReportSize(Vector3? size)
        {
            if (!size.HasValue)
            {
                spriteUsageFlags |= SpriteUsageFlags.CantDiscoveryAllUsageSize;
                return;
            }

            if (firstSizeReport || !maximumUsageSize.HasValue 
                                || size.Value.sqrMagnitude > maximumUsageSize.Value.sqrMagnitude)
            {
                maximumUsageSize = size;
                CheckForSizeFlags();
                firstSizeReport = false;
            }
        }

        public void CheckForSizeFlags()
        {
            if (!maximumUsageSize.HasValue) 
                return;
            
            spriteUsageFlags &= ~SpriteUsageFlags.UsedSmallerThanSpriteRect;
            spriteUsageFlags &= ~SpriteUsageFlags.UsedBiggerThanSpriteRect;

            Vector3 sizeDifference = new Vector3(maximumUsageSize.Value.x - SpriteSize.x,
                maximumUsageSize.Value.y - SpriteSize.y, 0);

            float differenceMagnitude = sizeDifference.magnitude / SpriteSize.magnitude;
            if (Mathf.Abs(differenceMagnitude) > 0.25f)
            {
                if (maximumUsageSize.Value.sqrMagnitude > SpriteSize.sqrMagnitude)
                {
                    if (SpriteAuditorUtility.CanTweakMaxSize(this))
                    {
                        spriteUsageFlags |= SpriteUsageFlags.UsedBiggerThanSpriteRect;
                    }
                    
                }
                else
                {
                    if (SpriteAuditorUtility.CanTweakMaxSize(this))
                    {
                        spriteUsageFlags |= SpriteUsageFlags.UsedSmallerThanSpriteRect;
                    }
                }
            }}

        private SpriteUseData GetOrCreateSpriteUsageData(GameObject instance, string usagePath)
        {
            int instanceID = instance.GetInstanceID();
            if (TryGetSpriteUsageData(instanceID, usagePath, out SpriteUseData spriteUseData))
                return spriteUseData;

            spriteUseData = new SpriteUseData(instance, instanceID, usagePath);
            usages.Add(spriteUseData);
            SpriteAuditorUtility.SetResultViewDirty();
            return spriteUseData;
        }

        private bool TryGetSpriteUsageData(int instanceID, string usagePath, out SpriteUseData spriteUseData)
        {
            int usagesCount = usages.Count;
            for (int i = 0; i < usagesCount; i++)
            {
                spriteUseData = usages[i];
                if (spriteUseData.InstanceID == instanceID
                || spriteUseData.HierarchyPaths.Contains(usagePath))
                    return true;
            }

            spriteUseData = null;
            return false;
        }


        public bool IsInsideAtlas()
        {
            return SpriteAtlas != null;
        }

        public bool HasWarnings()
        {
            if (spriteUsageFlags.HasFlag(SpriteUsageFlags.CantDiscoveryAllUsageSize))
                return true;
            
            if (spriteUsageFlags.HasFlag(SpriteUsageFlags.UsedBiggerThanSpriteRect))
                return true;

            if (spriteUsageFlags.HasFlag(SpriteUsageFlags.UsedSmallerThanSpriteRect))
                return true;
            
            if (spriteUsageFlags.HasFlag(SpriteUsageFlags.UsedOnDontDestroyOrUnknowScene))
                return true;

            return false;
            }

        public void PrepareForRun()
        {
            firstSizeReport = true;
        }
    }
}
