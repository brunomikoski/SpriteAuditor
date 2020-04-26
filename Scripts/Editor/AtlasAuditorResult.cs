using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

namespace BrunoMikoski.AtlasAudior
{
    [Serializable]
    public class AtlasAuditorResult 
    {
        [SerializeField]
        private Dictionary<string, HashSet<string>> sceneToSprites = new Dictionary<string, HashSet<string>>();
        [SerializeField]
        private HashSet<string> noSceneSprites = new HashSet<string>();
        
        [SerializeField] 
        private Dictionary<string, Dictionary<int, int>> spriteGUIDToInstanceIDToUseCount = new Dictionary<string, Dictionary<int, int>>();
        [SerializeField]
        private Dictionary<string, HashSet<string>> spriteGUIDtoUsePath = new Dictionary<string, HashSet<string>>();
        [SerializeField]
        private Dictionary<string, Vector3> spriteGUIDToMaximumSize = new Dictionary<string, Vector3>();
        
        
        //Caching
        private Dictionary<string, Sprite> spriteGUIDToSpriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, SceneAsset> sceneGUIDToSceneAssetCache = new Dictionary<string, SceneAsset>();
        public Dictionary<string, SceneAsset> SceneGUIDToSceneAssetCache => sceneGUIDToSceneAssetCache;


        private Dictionary<SceneAsset, Dictionary<SpriteAtlas, HashSet<Sprite>>> sceneToSpriteAtlasToSprites = new Dictionary<SceneAsset, Dictionary<SpriteAtlas, HashSet<Sprite>>>();
        public Dictionary<SceneAsset, Dictionary<SpriteAtlas, HashSet<Sprite>>> SceneToSpriteAtlasToSprites => sceneToSpriteAtlasToSprites;
        
        private Dictionary<SceneAsset, HashSet<Sprite>> sceneToSingleSprites = new Dictionary<SceneAsset, HashSet<Sprite>>();
        public Dictionary<SceneAsset, HashSet<Sprite>> SceneToSingleSprites => sceneToSingleSprites;
        
        private Dictionary<Sprite, HashSet<SceneAsset>> spriteToScenes = new Dictionary<Sprite, HashSet<SceneAsset>>();
        public Dictionary<Sprite, HashSet<SceneAsset>> SpriteToScenes => spriteToScenes;

        private Dictionary<SpriteAtlas, List<Sprite>> atlasToAllSprites = new Dictionary<SpriteAtlas, List<Sprite>>();
        public Dictionary<SpriteAtlas, List<Sprite>> AtlasToAllSprites => atlasToAllSprites;

        private Dictionary<Sprite, int> spriteToUseCount = new Dictionary<Sprite, int>();
        private Dictionary<Sprite, HashSet<string>> spriteToUseTransformPath = new Dictionary<Sprite, HashSet<string>>();
        public Dictionary<Sprite, HashSet<string>> SpriteToUseTransformPath => spriteToUseTransformPath;
        private Dictionary<Sprite, Vector3> spriteToMaximumSize = new Dictionary<Sprite, Vector3>();

        
        private Dictionary<SpriteAtlas, HashSet<Sprite>> atlasToUsedSprites = new Dictionary<SpriteAtlas, HashSet<Sprite>>();
        private Dictionary<SpriteAtlas, HashSet<Sprite>> atlasToNotUsedSprites = new Dictionary<SpriteAtlas, HashSet<Sprite>>();
        private HashSet<SpriteAtlas> notUsedAtlases = new HashSet<SpriteAtlas>();

        private bool isReferencesDirty;
        
        private bool isDataDirty;

        private Camera cachedCamera;
        private Camera Camera
        {
            get
            {
                if (cachedCamera == null)
                    cachedCamera = Camera.main;
                return cachedCamera;
            }
        }

        public bool IsDataDirty => isDataDirty;
        
        public void AddSprite(Sprite targetSprite, GameObject gameObject)
        {
            if (targetSprite == null)
                return;

            Scene targetScene = gameObject.scene;

            string sceneGUID = AssetDatabase.AssetPathToGUID(targetScene.path);
            string assetPath = AssetDatabase.GetAssetPath(targetSprite);

            string spriteGUID = AssetDatabase.AssetPathToGUID(assetPath);

            if (!spriteGUIDToInstanceIDToUseCount.ContainsKey(spriteGUID))
                spriteGUIDToInstanceIDToUseCount.Add(spriteGUID, new Dictionary<int, int>());

            if (!spriteGUIDtoUsePath.ContainsKey(spriteGUID))
                spriteGUIDtoUsePath.Add(spriteGUID, new HashSet<string>());

            int gameObjectInstanceID = gameObject.GetInstanceID();
            //If is the first time we are seeing this game object sprite combination
            if (!spriteGUIDToInstanceIDToUseCount[spriteGUID].ContainsKey(gameObjectInstanceID))
            {
                //Adding the "unique" usage count
                spriteGUIDToInstanceIDToUseCount[spriteGUID].Add(gameObjectInstanceID, 0);
                //Adding the unique path to this game object
                spriteGUIDtoUsePath[spriteGUID].Add(gameObject.transform.GetPath());
                
                //Incrementing the usage count
                int count = spriteGUIDToInstanceIDToUseCount[spriteGUID][gameObjectInstanceID];
                count++;
                spriteGUIDToInstanceIDToUseCount[spriteGUID][gameObjectInstanceID] = count;


                if (!spriteGUIDToMaximumSize.ContainsKey(spriteGUID))
                    spriteGUIDToMaximumSize.Add(spriteGUID, Vector2.zero);
                
                
                if (gameObject.transform is RectTransform rectTransform)
                {
                    RectTransform canvasRectTransform = (RectTransform) rectTransform.GetComponentInParent<Canvas>().transform;
                    
                    Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRectTransform, rectTransform);

                    if (bounds.size.sqrMagnitude > spriteGUIDToMaximumSize[spriteGUID].sqrMagnitude)
                        spriteGUIDToMaximumSize[spriteGUID] = bounds.size;
                }
                else
                {
                    SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        Vector3 finalSize = spriteRenderer.GetPixelSize(Camera);
                        
                        if (finalSize.sqrMagnitude > spriteGUIDToMaximumSize[spriteGUID].sqrMagnitude)
                            spriteGUIDToMaximumSize[spriteGUID] = finalSize;
                    }
                }
            }

            if (string.IsNullOrEmpty(sceneGUID))
            {
                noSceneSprites.Add(spriteGUID);   
            }
            else
            {
                if (!sceneToSprites.ContainsKey(sceneGUID))
                    sceneToSprites.Add(sceneGUID, new HashSet<string>());

                sceneToSprites[sceneGUID].Add(spriteGUID);
            }

            isReferencesDirty = true;
            isDataDirty = true;
        }

        public void AssignReferences()
        {
            if (!isReferencesDirty)
                return;
            
            sceneToSpriteAtlasToSprites.Clear();
            sceneToSingleSprites.Clear();
            spriteToScenes.Clear();
            spriteToUseCount.Clear();
            spriteToUseTransformPath.Clear();
            spriteToMaximumSize.Clear();
            
            CacheKnowAtlases();

            foreach (var sceneToSprites in sceneToSprites)
            {
                if (!TryGetSceneFromCache(sceneToSprites.Key, out SceneAsset scene))
                    continue;
                
                if (!sceneToSpriteAtlasToSprites.ContainsKey(scene))
                {
                    sceneToSpriteAtlasToSprites.Add(scene, new Dictionary<SpriteAtlas, HashSet<Sprite>>());
                    sceneToSingleSprites.Add(scene, new HashSet<Sprite>());
                }

                foreach (string spriteGUID in sceneToSprites.Value)
                {
                    if (!TryGetSpriteFromCache(spriteGUID, out Sprite sprite)) 
                        continue;
                    
                    if(!spriteToScenes.ContainsKey(sprite))
                        spriteToScenes.Add(sprite, new HashSet<SceneAsset>());

                    spriteToScenes[sprite].Add(scene);
                    
                    if (TryGetAtlasForSprite(sprite, out SpriteAtlas targetAtlas))
                    {
                        if(!sceneToSpriteAtlasToSprites[scene].ContainsKey(targetAtlas))
                            sceneToSpriteAtlasToSprites[scene].Add(targetAtlas, new HashSet<Sprite>());
                        
                        sceneToSpriteAtlasToSprites[scene][targetAtlas].Add(sprite);

                        if (!atlasToUsedSprites.ContainsKey(targetAtlas))
                            atlasToUsedSprites.Add(targetAtlas, new HashSet<Sprite>());

                        atlasToUsedSprites[targetAtlas].Add(sprite);
                    }
                    else
                    {
                        sceneToSingleSprites[scene].Add(sprite);
                    }
                }
            }
            
            foreach (var spriteGUIDToInstanceToCount in spriteGUIDToInstanceIDToUseCount)
            {
                if (!TryGetSpriteFromCache(spriteGUIDToInstanceToCount.Key, out var sprite)) 
                    continue;

                if (!spriteToUseCount.ContainsKey(sprite))
                    spriteToUseCount.Add(sprite, 0);

                foreach (var instanceIDToUsage in spriteGUIDToInstanceToCount.Value)
                {
                    int count = spriteToUseCount[sprite];
                    count += instanceIDToUsage.Value;
                    spriteToUseCount[sprite] = count;
                }
            }

            foreach (var spriteGUIDtoUsePath in spriteGUIDtoUsePath)
            {
                if (!TryGetSpriteFromCache(spriteGUIDtoUsePath.Key, out var sprite)) 
                    continue;

                if (!spriteToUseTransformPath.ContainsKey(sprite))
                    spriteToUseTransformPath.Add(sprite, new HashSet<string>());

                foreach (string path in spriteGUIDtoUsePath.Value)
                    spriteToUseTransformPath[sprite].Add(path);
            }


            foreach (var spriteGUIDtoMaximumSize in spriteGUIDToMaximumSize)
            {
                if (!TryGetSpriteFromCache(spriteGUIDtoMaximumSize.Key, out var sprite)) 
                    continue;
                spriteToMaximumSize.Add(sprite, spriteGUIDtoMaximumSize.Value);
            }
            
            
            foreach (var spritesByAtlas in atlasToAllSprites)
            {
                if (atlasToUsedSprites.ContainsKey(spritesByAtlas.Key))
                {
                    foreach (Sprite sprite in spritesByAtlas.Value)
                    {
                        if (!atlasToUsedSprites[spritesByAtlas.Key].Contains(sprite))
                        {
                            if(!atlasToNotUsedSprites.ContainsKey(spritesByAtlas.Key))
                                atlasToNotUsedSprites.Add(spritesByAtlas.Key, new HashSet<Sprite>());

                            atlasToNotUsedSprites[spritesByAtlas.Key].Add(sprite);
                        }
                    }
                }
                else
                {
                    notUsedAtlases.Add(spritesByAtlas.Key);
                }
            }
            
            
            
            isReferencesDirty = false;
        }

        private bool TryGetSceneFromCache(string sceneGUID, out SceneAsset sceneAsset)
        {
            if (sceneGUIDToSceneAssetCache.TryGetValue(sceneGUID, out sceneAsset))
                return true;

            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
            sceneAsset = (SceneAsset) AssetDatabase.LoadAssetAtPath(scenePath, typeof(SceneAsset));

            if (sceneAsset == null)
                return false;
            
            sceneGUIDToSceneAssetCache.Add(sceneGUID, sceneAsset);
            return true;
        }

        private bool TryGetSpriteFromCache(string spriteGUID, out Sprite sprite)
        {
            if (spriteGUIDToSpriteCache.TryGetValue(spriteGUID, out sprite))
                return true;

            string spritePath = AssetDatabase.GUIDToAssetPath(spriteGUID);
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (sprite == null)
                return false;

            spriteGUIDToSpriteCache.Add(spriteGUID, sprite);
            return true;
        }

        private bool TryGetAtlasForSprite(Sprite targetSprite, out SpriteAtlas resultSpriteAtlas)
        {
           foreach (var atlasToSprite in atlasToAllSprites)
           {
               for (int i = 0; i < atlasToSprite.Value.Count; i++)
               {
                   if (atlasToSprite.Value[i] == targetSprite)
                   {
                       resultSpriteAtlas = atlasToSprite.Key;
                       return true;
                   }
               }
           }

           resultSpriteAtlas = null;
           return false;
        }

        private void CacheKnowAtlases()
        {
            string[] atlasGUIDs = AssetDatabase.FindAssets("t:SpriteAtlas");

            if (atlasGUIDs.Length == atlasToAllSprites.Count)
                return;
            
            atlasToAllSprites.Clear();
            
            for (int i = 0; i < atlasGUIDs.Length; i++)
            {
                SpriteAtlas atlas =
                    AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(atlasGUIDs[i]));

                if (atlas.isVariant)
                    continue;
                
                atlasToAllSprites.Add(atlas, atlas.GetAllSprites());
            }
        }

        public bool TryGetSpriteSceneUsages(Sprite sprite, out HashSet<SceneAsset> sceneAssets)
        {
            return spriteToScenes.TryGetValue(sprite, out sceneAssets);
        }

        public void SetDataDirty(bool isDirty)
        {
            isDataDirty = isDirty;
        }

        public void SetReferencesDirty(bool isDirty)
        {
            isReferencesDirty = isDirty;
        }

        public int GetSpriteUseCount(Sprite sprite)
        {
            if (spriteToUseCount.TryGetValue(sprite, out int count))
                return count;

            return 0;
        }

        public Vector3 GetSpriteMaxUseSize(Sprite sprite)
        {
            if (spriteToMaximumSize.TryGetValue(sprite, out Vector3 maxSize))
                return maxSize;
            
            return Vector3.zero;
        }

        public void ClearSpriteToMaxSize(Sprite targetSprite)
        {
            string assetPath = AssetDatabase.GetAssetPath(targetSprite);

            string spriteGUID = AssetDatabase.AssetPathToGUID(assetPath);

            spriteGUIDToMaximumSize.Remove(spriteGUID);
            isDataDirty = true;
        }
    }
}
