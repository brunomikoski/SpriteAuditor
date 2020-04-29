using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BrunoMikoski.SpriteAuditor
{
    [Serializable]
    public class SpriteAuditorResult 
    {
        [SerializeField]
        private Dictionary<string, HashSet<string>> sceneToSprites = new Dictionary<string, HashSet<string>>();
        [SerializeField]
        private HashSet<string> noSceneSprites = new HashSet<string>();
        
        
        [SerializeField] 
        private Dictionary<string, List<int>> spriteGUIDToInstancesIDs = new Dictionary<string, List<int>>();
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

        private Dictionary<Sprite, HashSet<string>> spriteToUseTransformPath = new Dictionary<Sprite, HashSet<string>>();
        public Dictionary<Sprite, HashSet<string>> SpriteToUseTransformPath => spriteToUseTransformPath;
        private Dictionary<Sprite, Vector3> spriteToMaximumSize = new Dictionary<Sprite, Vector3>();

        
        private Dictionary<SpriteAtlas, HashSet<Sprite>> atlasToUsedSprites = new Dictionary<SpriteAtlas, HashSet<Sprite>>();
        public Dictionary<SpriteAtlas, HashSet<Sprite>> AtlasToUsedSprites => atlasToUsedSprites;

        private Dictionary<SpriteAtlas, HashSet<Sprite>> atlasToNotUsedSprites = new Dictionary<SpriteAtlas, HashSet<Sprite>>();
        public Dictionary<SpriteAtlas, HashSet<Sprite>> AtlasToNotUsedSprites => atlasToNotUsedSprites;
        
        private Dictionary<SpriteAtlas, float> atlasToScale = new Dictionary<SpriteAtlas, float>();
        public Dictionary<SpriteAtlas, float> AtlasToScale => atlasToScale;

        private HashSet<SpriteAtlas> notUsedAtlases = new HashSet<SpriteAtlas>();
        public HashSet<SpriteAtlas> NotUsedAtlases => notUsedAtlases;

        private bool isReferencesDirty;
        private bool isSaveDataDirty;
        public bool IsSaveDataDirty => isSaveDataDirty;

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
        
        public void AddSprite(Sprite targetSprite, GameObject gameObject, Vector3 spriteUsageSize)
        {
            bool dataChanged = false;

            Scene targetScene = gameObject.scene;

            string sceneGUID = AssetDatabase.AssetPathToGUID(targetScene.path);
            string spriteGUID = targetSprite.GetGUID();

            if (!spriteGUIDToInstancesIDs.ContainsKey(spriteGUID))
            {
                spriteGUIDToInstancesIDs.Add(spriteGUID, new List<int>());
                spriteGUIDtoUsePath.Add(spriteGUID, new HashSet<string>());
            }

            int gameObjectInstanceID = gameObject.GetInstanceID();
            
            //If is the first time we are seeing this game object sprite combination
            if (!spriteGUIDToInstancesIDs[spriteGUID].Contains(gameObjectInstanceID))
            {
                dataChanged = true;
                
                //Adding the "unique" usage count
                spriteGUIDToInstancesIDs[spriteGUID].Add(gameObjectInstanceID);
                //Adding the unique path to this game object
                spriteGUIDtoUsePath[spriteGUID].Add(gameObject.transform.GetPath());
            }

            if (!spriteGUIDToMaximumSize.ContainsKey(spriteGUID))
            {
                spriteGUIDToMaximumSize.Add(spriteGUID, Vector2.zero);
                dataChanged = true;
            }

            if (spriteUsageSize.sqrMagnitude > spriteGUIDToMaximumSize[spriteGUID].sqrMagnitude)
            {
                spriteGUIDToMaximumSize[spriteGUID] = spriteUsageSize;
                dataChanged = true;
            }

            if (string.IsNullOrEmpty(sceneGUID))
            {
                if (noSceneSprites.Add(spriteGUID))
                    dataChanged = true;
            }
            else
            {
                if (!sceneToSprites.ContainsKey(sceneGUID))
                    sceneToSprites.Add(sceneGUID, new HashSet<string>());

                if (sceneToSprites[sceneGUID].Add(spriteGUID))
                    dataChanged = true;
            }

            if (dataChanged)
            {
                isReferencesDirty = true;
                isSaveDataDirty = true;
            }
        }

        public void AssignReferences()
        {
            if (!isReferencesDirty)
                return;
            
            sceneToSpriteAtlasToSprites.Clear();
            sceneToSingleSprites.Clear();
            spriteToScenes.Clear();
            spriteToUseTransformPath.Clear();
            spriteToMaximumSize.Clear();
            

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

        public void CacheKnowAtlases()
        {
            atlasToAllSprites.Clear();
            atlasToScale.Clear();

            string[] atlasGUIDs = AssetDatabase.FindAssets("t:SpriteAtlas");
            
            for (int i = 0; i < atlasGUIDs.Length; i++)
            {
                SpriteAtlas atlas =
                    AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(atlasGUIDs[i]));

                if (!atlas.IsIncludedInBuild())
                    continue;

                if (atlas.isVariant)
                {
                    if (atlas.TryGetMasterAtlas(out SpriteAtlas masterAtlas))
                    {
                        atlasToScale.Add(atlas, atlas.GetVariantScale());
                        atlasToAllSprites.Add(atlas, masterAtlas.GetAllSprites());
                    }
                }
                else
                {
                    atlasToAllSprites.Add(atlas, atlas.GetAllSprites());
                    atlasToScale.Add(atlas, 1.0f);
                }
            }
        }

        public bool TryGetSpriteSceneUsages(Sprite sprite, out HashSet<SceneAsset> sceneAssets)
        {
            return spriteToScenes.TryGetValue(sprite, out sceneAssets);
        }

        public void SetDataDirty(bool isDirty)
        {
            isSaveDataDirty = isDirty;
        }

        public void SetReferencesDirty(bool isDirty)
        {
            isReferencesDirty = isDirty;
        }

        public int GetSpriteUseCount(Sprite sprite)
        {
            if (spriteGUIDToInstancesIDs.TryGetValue(sprite.GetGUID(), out List<int> uniqueUsages))
                return uniqueUsages.Count;
            
            return 0;
        }

        public Vector3 GetSpriteMaxUseSize(Sprite sprite)
        {
            if (spriteToMaximumSize.TryGetValue(sprite, out Vector3 maxSize))
                return maxSize;
            
            return Vector3.zero;
        }

        public void ClearAllSpritesKnowSizes()
        {
            spriteGUIDToMaximumSize.Clear();
            isSaveDataDirty = true;
        }
        
        public void ClearAtlasesCache()
        {
            CacheKnowAtlases();
        }
    }
}
