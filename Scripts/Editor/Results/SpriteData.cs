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
        [SerializeField] 
        private string spriteTextureGUID;

        [SerializeField] 
        private string spriteAtlasGUID;

        [SerializeField] 
        private string spriteName;

        [SerializeField] 
        private Vector3 minimumUsageSize = Vector3.positiveInfinity;
        public Vector3 MinimumUsageSize => minimumUsageSize;

        [SerializeField] 
        private Vector3 maximumUsageSize = Vector3.zero;
        public Vector3 MaximumUsageSize => maximumUsageSize;

        [SerializeField] 
        private Vector3 averageUsageSize = Vector3.zero;
        public Vector3 AverageUsageSize => averageUsageSize;
        
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
        private SpriteUsageFlags spriteUsageFlags = 0;

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

        public bool IsValid()
        {
            if (spriteUsageFlags.HasFlag(SpriteUsageFlags.DefaultUnityAsset))
                return false;

            if (Sprite == null)
                return false;
            
            return true;
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
        

        public SpriteData(Sprite targetSprite)
        {
            cachedSprite = targetSprite;
            spriteTexturePath = AssetDatabase.GetAssetPath(cachedSprite);
            spriteTextureGUID = AssetDatabase.AssetPathToGUID(spriteTexturePath);
            spriteName = targetSprite.name;

            if (string.Equals(spriteTexturePath, "Resources/unity_builtin_extra"))
                spriteUsageFlags |= SpriteUsageFlags.DefaultUnityAsset;
            
            if (AtlasCacheUtility.TryGetAtlasForSprite(targetSprite, out SpriteAtlas spriteAtlas))
            {
                cachedSpriteAtlas = spriteAtlas;
                spriteAtlasGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(spriteAtlas));
                atlasScale = AtlasCacheUtility.GetAtlasScale(spriteAtlas);
            }


            TextureImporter spriteImporter = AssetImporter.GetAtPath(spriteTexturePath) as TextureImporter;

            if (spriteImporter != null)
            {
                if (spriteImporter.spriteImportMode == SpriteImportMode.Multiple)
                    spriteUsageFlags |= SpriteUsageFlags.HasMultipleSpritesRect;
            }
        }

        public void ReportUse(GameObject instance, Vector3 size)
        {
            string usagePath = instance.transform.GetPath();
            SpriteUseData spriteUsageData = GetOrCreateSpriteUsageData(instance.GetInstanceID(), usagePath);
            Scene instanceScene = instance.scene;
            ReportScene(instanceScene);
            spriteUsageData.ReportPath(usagePath, instanceScene);

            if (size == Vector3.zero)
            {
                spriteUsageFlags |= SpriteUsageFlags.CantDiscoveryUsageSize;
                return;
            }

            ReportSize(size);
        }

        private void ReportScene(Scene scene)
        {
            if (scene.buildIndex == -1 || string.IsNullOrEmpty(scene.path))
            {
                if (scene.buildIndex == -1)
                    spriteUsageFlags |= SpriteUsageFlags.UsedOnDontDestroyOnLoadScene;
                
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene openScene = SceneManager.GetSceneAt(i);
                    if (!openScene.isLoaded)
                        continue;
                    if (!openScene.IsValid())
                        continue;

                    scenesPath.Add(openScene.path);
                }
            }
            else
            {
                scenesPath.Add(scene.path);
            }
        }

        public void ReportUse(SpriteRenderer spriteRenderer)
        {
            Vector3 size = Vector3.zero;
            if (CullingMaskUtility.TryGetCameraForGameObject(spriteRenderer.gameObject, out Camera camera))
                size = spriteRenderer.GetPixelSize(camera);

            ReportUse(spriteRenderer.gameObject, size);
        }

        public void ReportUse(SpriteMask spriteMask)
        {
            Vector3 size = Vector3.zero;
            if (CullingMaskUtility.TryGetCameraForGameObject(spriteMask.gameObject, out Camera camera))
                size = spriteMask.GetPixelSize(camera);

            ReportUse(spriteMask.gameObject, size);
        }

        public void ReportUse(Image image)
        {
            Vector3 size = Vector3.zero;
            if (image.type != Image.Type.Tiled && image.type != Image.Type.Sliced)
                size = image.GetPixelSize();

            ReportUse(image.gameObject, size);
        }

        private void ReportSize(Vector3 size)
        {
            if (size.sqrMagnitude > maximumUsageSize.sqrMagnitude)
            {
                maximumUsageSize = size;
                CheckForSizeFlags();
            }

            if (size.sqrMagnitude < minimumUsageSize.sqrMagnitude)
            {
                minimumUsageSize = size;
                CheckForSizeFlags();
            }

            averageUsageSize = (maximumUsageSize + minimumUsageSize) * 0.5f;
        }

        private void CheckForSizeFlags()
        {
            if (maximumUsageSize.sqrMagnitude > Sprite.rect.size.sqrMagnitude)
                spriteUsageFlags |= SpriteUsageFlags.UsedBiggerThanSpriteRect;
            else 
                spriteUsageFlags &= SpriteUsageFlags.UsedBiggerThanSpriteRect;
            
            if (minimumUsageSize.sqrMagnitude < Sprite.rect.size.sqrMagnitude)
                spriteUsageFlags |= SpriteUsageFlags.UsedSmallerThanSpriteRect;
            else
                spriteUsageFlags &= SpriteUsageFlags.UsedSmallerThanSpriteRect;
        }

        private SpriteUseData GetOrCreateSpriteUsageData(int instanceID, string usagePath)
        {
            if (TryGetSpriteUsageData(instanceID,usagePath, out SpriteUseData spriteUseData))
                return spriteUseData;

            spriteUseData = new SpriteUseData(instanceID, usagePath);
            usages.Add(spriteUseData);
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
    }
}