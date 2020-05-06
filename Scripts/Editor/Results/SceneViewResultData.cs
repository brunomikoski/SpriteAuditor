using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace BrunoMikoski.SpriteAuditor
{
    public sealed class SceneViewResultData : ResultViewDataBase
    {
        private SceneAsset[] uniqueUsedScenes;

        private readonly Dictionary<SceneAsset, SpriteData[]> sceneToSingleSprites = new Dictionary<SceneAsset, SpriteData[]>();

        private readonly Dictionary<SceneAsset, Dictionary<SpriteAtlas, HashSet<SpriteData>>> sceneToAtlasToUsedSprites
            = new Dictionary<SceneAsset, Dictionary<SpriteAtlas, HashSet<SpriteData>>>();
        
        private bool hasResults;

        public override void GenerateResults(SpriteDatabase spriteDatabase, ResultsFilter currentFilter)
        {
            //This method is kinda of dumb doing a lot of repetitive task, but its just easier to read this way
            HashSet<SceneAsset> usedScenes = new HashSet<SceneAsset>();

            SpriteData[] validSprites = spriteDatabase.GetFilteredSprites(currentFilter);
            
            for (int i = 0; i < validSprites.Length; i++)
            {
                SpriteData spriteData = validSprites[i];
                
                IEnumerable<SceneAsset> uniqueScenes = spriteData.SceneAssets.Distinct();
                foreach (SceneAsset uniqueScene in uniqueScenes)
                {
                    usedScenes.Add(uniqueScene);
                }
            }
            
            sceneToSingleSprites.Clear();
            foreach (SceneAsset sceneAsset in usedScenes)
            {
                sceneToSingleSprites.Add(sceneAsset, validSprites
                    .Where(spriteData => !spriteData.IsInsideAtlas())
                    .Where(spriteData => spriteData.SceneAssets.Contains(sceneAsset)).Distinct().ToArray());
            }

            sceneToAtlasToUsedSprites.Clear();
            foreach (SpriteData spriteData in validSprites)
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
            
            sceneToSingleSprites.Add(SpriteAuditorUtility.DontDestroyOnLoadSceneAsset, validSprites
                .Where(data => data.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsedOnDontDestroyOrUnknowScene) 
                               && !data.IsInsideAtlas()).Distinct().ToArray());
            
            sceneToAtlasToUsedSprites.Add(SpriteAuditorUtility.DontDestroyOnLoadSceneAsset, new Dictionary<SpriteAtlas, HashSet<SpriteData>>());

            IEnumerable<SpriteData> spritesInsideAtlas = validSprites
                .Where(data => data.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsedOnDontDestroyOrUnknowScene) &&
                               data.IsInsideAtlas()).Distinct();

            foreach (SpriteData spriteInsideAtlas in spritesInsideAtlas)
            {
                if (!sceneToAtlasToUsedSprites[SpriteAuditorUtility.DontDestroyOnLoadSceneAsset]
                    .ContainsKey(spriteInsideAtlas.SpriteAtlas))
                {
                    sceneToAtlasToUsedSprites[SpriteAuditorUtility.DontDestroyOnLoadSceneAsset]
                        .Add(spriteInsideAtlas.SpriteAtlas, new HashSet<SpriteData>());
                }

                sceneToAtlasToUsedSprites[SpriteAuditorUtility.DontDestroyOnLoadSceneAsset]
                    [spriteInsideAtlas.SpriteAtlas].Add(spriteInsideAtlas);
            }

            if (sceneToSingleSprites[SpriteAuditorUtility.DontDestroyOnLoadSceneAsset].Length > 0
                || sceneToAtlasToUsedSprites[SpriteAuditorUtility.DontDestroyOnLoadSceneAsset].Count > 0)
            {
                usedScenes.Add(SpriteAuditorUtility.DontDestroyOnLoadSceneAsset);
            }

            uniqueUsedScenes = usedScenes.ToArray();
            hasResults = true;
        }

        public override void DrawResults(SpriteDatabase spriteDatabase)
        {
            if (!hasResults)
                return;
            
            for (int i = 0; i < uniqueUsedScenes.Length; i++)
            {
                SceneAsset sceneAsset = uniqueUsedScenes[i];
                
                EditorGUILayout.BeginVertical("Box");

                if (SpriteAuditorGUIUtility.DrawObjectFoldout(sceneAsset, sceneAsset.name))
                {
                    EditorGUI.indentLevel++;
                    if (sceneToSingleSprites[sceneAsset].Length > 0)
                    {
                        EditorGUILayout.BeginVertical("Box");
                        
                        if(SpriteAuditorGUIUtility.DrawStringFoldout($"Sprites Without Atlas [{sceneToSingleSprites[sceneAsset].Length}] ", $"{sceneAsset.name}_SceneViewSpritesWithoutAtlas"))
                        {
                            EditorGUI.indentLevel++;
                            foreach (SpriteData spriteData in sceneToSingleSprites[sceneAsset])
                            {
                                DrawSpriteDataField(spriteData);
                            }

                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.EndVertical();
                    }

                    if (sceneToAtlasToUsedSprites.ContainsKey(sceneAsset))
                    {
                        foreach (var atlasToUSedSprites in sceneToAtlasToUsedSprites[sceneAsset])
                        {
                            EditorGUILayout.BeginVertical("Box");

                            if (SpriteAuditorGUIUtility.DrawObjectFoldout(atlasToUSedSprites.Key,
                                $"{VisualizationType.Scene.ToString()}_{atlasToUSedSprites.Key}"))
                            {
                                EditorGUI.indentLevel++;
                                foreach (SpriteData spriteData in sceneToAtlasToUsedSprites[sceneAsset][
                                    atlasToUSedSprites.Key])
                                {
                                    DrawSpriteDataField(spriteData);
                                }

                                EditorGUI.indentLevel--;
                            }

                            EditorGUILayout.EndVertical();
                        }
                    }

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
        }
    }
}
