using System;
using System.Collections.Generic;
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
        private string spriteGUID;

        [SerializeField] 
        private string spriteAtlasGUID;

        [SerializeField]
        private Vector3 minimumUsageSize = Vector3.zero;
        public Vector3 MinimumUsageSize => minimumUsageSize;
        
        [SerializeField]
        private Vector3 maximumUsageSize = Vector3.zero;
        public Vector3 MaximumUsageSize => maximumUsageSize;

        [SerializeField]
        private Vector3 averageUsageSize = Vector3.zero;
        public Vector3 AverageUsageSize => averageUsageSize;


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
                if (cachedSceneAssets.Count != scenesPath.Count)
                {
                    cachedSceneAssets.Clear();
                    foreach (string scenePath in scenesPath)
                    {
                        cachedSceneAssets.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath));
                    }
                }

                return cachedSceneAssets;
            }
        }

        [SerializeField]
        private SpriteUsageIssues spriteUsageIssues;

        [SerializeField]
        private float atlasScale = 1;
        
        private bool usedOnDontDestroyOnLoadScene;
        public bool UsedOnDontDestroyOnLoadScene => usedOnDontDestroyOnLoadScene;

        private Sprite cachedSprite;
        public Sprite Sprite
        {
            get
            {
                if (cachedSprite == null)
                {
                    if (!string.IsNullOrEmpty(spriteGUID))
                    {
                        cachedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGUID));
                    }
                }
                return cachedSprite;
            }
        }

        private SpriteAtlas cachedSpriteAtlas;

        public Vector2 SpriteSize => Sprite.rect.size * atlasScale;

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
            spriteGUID =  AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(cachedSprite));

            if (AtlasUtility.TryGetAtlasForSprite(targetSprite, out SpriteAtlas spriteAtlas))
            {
                cachedSpriteAtlas = spriteAtlas;
                atlasScale = AtlasUtility.GetAtlasScale(spriteAtlas);
            }
        }

        public void ReportUse(GameObject instance, Vector3 size)
        {
            SpriteUseData spriteUsageData = GetOrCreateSpriteUsageData(instance.GetInstanceID());
            Scene instanceScene = instance.scene;
            ReportScene(instanceScene);
            spriteUsageData.ReportPath(instance.transform.GetPath(), instanceScene);
            ReportSize(size);
        }

        private void ReportScene(Scene scene)
        {
            if (scene.buildIndex == -1)
            {
                
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
            if (image.type == Image.Type.Tiled || image.type == Image.Type.Sliced)
            {
                spriteUsageIssues |= SpriteUsageIssues.CantDiscoveryUsageSize;
                return;
            }

            size = image.GetPixelSize();

            ReportUse(image.gameObject, size);
        }

        private void ReportSize(Vector3 size)
        {
            //TODO adding here the flags if the sprite is used bigger or smaller or fine
            if (size.sqrMagnitude > maximumUsageSize.sqrMagnitude)
                maximumUsageSize = size;
            else if (size.sqrMagnitude < minimumUsageSize.sqrMagnitude)
                minimumUsageSize = size;

            averageUsageSize = (maximumUsageSize + minimumUsageSize) * 0.5f;
        }

        private SpriteUseData GetOrCreateSpriteUsageData(int instanceID)
        {
            if (TryGetSpriteUsageData(instanceID, out SpriteUseData spriteUseData))
                return spriteUseData;
            
            spriteUseData = new SpriteUseData(instanceID);
            usages.Add(spriteUseData);
            return spriteUseData;
        }

        private bool TryGetSpriteUsageData(int instanceID, out SpriteUseData spriteUseData)
        {
            int usagesCount = usages.Count;
            for (int i = 0; i < usagesCount; i++)
            {
                spriteUseData = usages[i];
                if (spriteUseData.InstanceID == instanceID)
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
