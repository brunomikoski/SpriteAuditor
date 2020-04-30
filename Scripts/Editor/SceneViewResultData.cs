using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.U2D;

namespace BrunoMikoski.SpriteAuditor
{
    public sealed class SceneViewResultData : ResultViewDataBase
    {
        public SceneAsset[] SceneAssets;
        public bool HasDontDestroyOnLoadSceneData; 
        
        public readonly Dictionary<SceneAsset, SpriteData[]> sceneToSingleSprites = new Dictionary<SceneAsset, SpriteData[]>();
        public readonly Dictionary<SceneAsset, Dictionary<SpriteAtlas, HashSet<SpriteData>>> sceneToAtlasToUsedSprites = new Dictionary<SceneAsset, Dictionary<SpriteAtlas, HashSet<SpriteData>>>();

        public override void GenerateResults(SpriteDatabase spriteDatabase)
        {
            //This method is kinda of dumb doing a lot of repetitive task, but its just easier to read this way
            
            List<SceneAsset> usedScenes = new List<SceneAsset>();

            for (int i = 0; i < spriteDatabase.SpritesData.Count; i++)
            {
                SpriteData spriteData = spriteDatabase.SpritesData[i];
                if (spriteData.UsedOnDontDestroyOnLoadScene)
                    HasDontDestroyOnLoadSceneData = true;

                usedScenes.AddRange(spriteData.SceneAssets.Distinct());
            }

            SceneAssets = usedScenes.ToArray();

            
            sceneToSingleSprites.Clear();
            for (int i = 0; i < SceneAssets.Length; i++)
            {
                SceneAsset sceneAsset = SceneAssets[i];
                sceneToSingleSprites.Add(sceneAsset, spriteDatabase.SpritesData
                    .Where(spriteData => !spriteData.IsInsideAtlas())
                    .Where(spriteData => spriteData.SceneAssets.Contains(sceneAsset)).Distinct().ToArray());
            }

            sceneToAtlasToUsedSprites.Clear();
            foreach (SpriteData spriteData in spriteDatabase.SpritesData)
            {
                if (!spriteData.IsInsideAtlas())
                    continue;

                foreach (SceneAsset sceneAsset  in spriteData.SceneAssets)
                {
                    if(!sceneToAtlasToUsedSprites.ContainsKey(sceneAsset))
                        sceneToAtlasToUsedSprites.Add(sceneAsset, new Dictionary<SpriteAtlas, HashSet<SpriteData>>());
                    
                    SpriteAtlas spriteAtlas = spriteData.SpriteAtlas;
                    if (!sceneToAtlasToUsedSprites[sceneAsset].ContainsKey(spriteAtlas))
                        sceneToAtlasToUsedSprites[sceneAsset].Add(spriteAtlas, new HashSet<SpriteData>());

                    sceneToAtlasToUsedSprites[sceneAsset][spriteAtlas].Add(spriteData);
                }
            }
        }
    }
}