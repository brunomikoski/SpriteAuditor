﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace BrunoMikoski.SpriteAuditor
{
    public sealed class SceneResultView : BaseResultView
    {
        [Flags]
        private enum Filter
        {
            UsedSmallerThanSpriteSize = 1 << 0,
            UsedBiggerThanSpriteSize = 1 << 1,
            UsedOnlyOnOneScenes = 1 << 2,
            UnableToDetectAllSizes = 1 << 3,
            SingleSprites = 1 << 4,
            MultipleSprites = 1 << 5,
            InsideAtlasSprites = 1 << 6,
            InsideScaledAtlasVariant = 1 << 7
        }
        
        private SceneAsset[] uniqueUsedScenes;

        private readonly Dictionary<SceneAsset, SpriteData[]> sceneToSingleSprites = new Dictionary<SceneAsset, SpriteData[]>();

        private readonly Dictionary<SceneAsset, Dictionary<SpriteAtlas, HashSet<SpriteData>>> sceneToAtlasToUsedSprites
            = new Dictionary<SceneAsset, Dictionary<SpriteAtlas, HashSet<SpriteData>>>();

        private Filter currentFilter;

        public override void DrawFilterOptions()
        {
            EditorGUI.BeginChangeCheck();
            currentFilter = (Filter) EditorGUILayout.EnumFlagsField("Filter", currentFilter);
            if (EditorGUI.EndChangeCheck())
                SpriteAuditorUtility.SetResultViewDirty();
        }

        private bool MatchFilter(SpriteData data)
        {
            if (!MatchSearch(data.Sprite.name))
                return false;
            
            if (currentFilter.HasFlag(Filter.UsedSmallerThanSpriteSize))
            {
                if (!data.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsedSmallerThanSpriteRect))
                    return false;
            }

            if (currentFilter.HasFlag(Filter.UsedBiggerThanSpriteSize))
            {
                if (!data.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsedBiggerThanSpriteRect))
                    return false;
            }
            
            if (currentFilter.HasFlag(Filter.UsedOnlyOnOneScenes))
            {
                if (data.SceneAssets.Count > 1)
                    return false;
            }

            if (currentFilter.HasFlag(Filter.UnableToDetectAllSizes))
            {
                if (!data.SpriteUsageFlags.HasFlag(SpriteUsageFlags.CantDiscoveryAllUsageSize))
                    return false;
            }

            if (currentFilter.HasFlag(Filter.SingleSprites))
            {
                if (data.TextureImporter == null || data.TextureImporter.spriteImportMode != SpriteImportMode.Single)
                    return false;
            }
            
            if (currentFilter.HasFlag(Filter.MultipleSprites))
            {
                if (data.TextureImporter == null || data.TextureImporter.spriteImportMode != SpriteImportMode.Multiple)
                    return false;
            }
            
            if (currentFilter.HasFlag(Filter.InsideAtlasSprites))
            {
                if(!data.IsInsideAtlas())
                    return false;
            }

            if (currentFilter.HasFlag(Filter.InsideScaledAtlasVariant))
            {
                if (!data.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsingScaledAtlasSize))
                    return false;
            }

            return true;
        }
        public override void GenerateResults(SpriteDatabase spriteDatabase)
        {
            //This method is kinda of dumb doing a lot of repetitive task, but its just easier to read this way
            HashSet<SceneAsset> usedScenes = new HashSet<SceneAsset>();

            SpriteData[] validSprites = spriteDatabase.SpritesData
                .Where(ValidSpriteData)
                .Where(MatchFilter)
                .OrderBy(data => data.Sprite.name)
                .ToArray();
            
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
        }

        protected override void DrawResultsInternal(SpriteDatabase spriteDatabase)
        {
            if (uniqueUsedScenes == null)
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

                            SpriteAuditorUtility.DrawDefaultSelectionOptions(sceneToSingleSprites[sceneAsset]
                                .Select(spriteData => spriteData.Sprite).Cast<Object>().ToList());
                            EditorGUI.indentLevel--;
                        }
                        EditorGUILayout.EndVertical();
                    }

                    if (sceneToAtlasToUsedSprites.ContainsKey(sceneAsset))
                    {
                        foreach (var atlasToUSedSprites in sceneToAtlasToUsedSprites[sceneAsset])
                        {
                            EditorGUILayout.BeginVertical("Box");

                            {
                                if (SpriteAuditorGUIUtility.DrawObjectFoldout(atlasToUSedSprites.Key,
                                    $"{VisualizationType.Scene.ToString()}_{atlasToUSedSprites.Key}"))
                                {
                                    EditorGUI.indentLevel++;
                                    foreach (SpriteData spriteData in sceneToAtlasToUsedSprites[sceneAsset][
                                        atlasToUSedSprites.Key])
                                    {
                                        DrawSpriteDataField(spriteData);
                                    }

                                    SpriteAuditorUtility.DrawDefaultSelectionOptions(
                                        sceneToAtlasToUsedSprites[sceneAsset][atlasToUSedSprites.Key]
                                            .Select(spriteData => spriteData.Sprite).Cast<Object>().ToList());
                                    EditorGUI.indentLevel--;
                                }
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
