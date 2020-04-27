﻿using System;
using System.Collections.Generic;
using BrunoMikoski.SpriteAuditor.Serialization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.SpriteAuditor
{
    public class SpriteAuditorWindow : EditorWindow
    {
        private const string ATLAS_AUDITOR_STORAGE_KEY = "ATLAS_AUDITOR_STORAGE_KEY";
        private static string[] VISUALIZATION_NAMES = {"Scene View", "Atlas View"};
        
        private const string ATLAS_VIEW_KEY = "ATLAS_VIEW_KEY";
        private const string SCENE_VIEW_KEY = "SCENE_VIEW_KEY";

        private bool isRecording;
        
        private SpriteFinder spriteFinder = new SpriteFinder();
        
        [NonSerialized]
        private SpriteAuditorResult cachedSpriteAuditorResult;
        private SpriteAuditorResult SpriteAuditorResult
        {
            get
            {
                if (cachedSpriteAuditorResult == null)
                    LoadOrCreateAtlasResult();
                return cachedSpriteAuditorResult;
            }
        }

        private Dictionary<string, bool> keyToFoldout = new Dictionary<string, bool>();

        private bool dontDestroyOnLoadFoldout;
        private bool scenesFoldout;
        private bool atlasesFoldout;

        private Vector2 scrollPosition = Vector2.zero;

        private bool recordOnPlay = true;
        private VisualizationType visualizationType = VisualizationType.Scene;
        private SpriteAuditorForwarder spriteAuditorForwarder;
        private bool showSpritesWithoutAtlas;
        private float spriteUsageSizeThreshold = 0.25f;

        [MenuItem("Tools/Sprite Auditor")]
        public static void OpenWindow()
        {
            SpriteAuditorWindow window = GetWindow<SpriteAuditorWindow>("Atlas Auditor");
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
        
        private void OnPlayModeChanged(PlayModeStateChange obj)
        {
            if (EditorApplication.isPlaying)
            {
                if (recordOnPlay)
                    StartRecording();
            }
            else
            {
                StopRecording();
            }
            SpriteAuditorResult?.SetReferencesDirty(true);
        }

        private void LoadOrCreateAtlasResult()
        {
            string storedJson = EditorPrefs.GetString(ATLAS_AUDITOR_STORAGE_KEY, string.Empty);
            if (!string.IsNullOrEmpty(storedJson))
            {
                cachedSpriteAuditorResult = new SpriteAuditorResult();
                JsonWrapper.FromJson(storedJson, ref cachedSpriteAuditorResult);
            }
            else
            {
                cachedSpriteAuditorResult = new SpriteAuditorResult();
            }
        }

        private void SaveAtlasResult()
        {
            if (!SpriteAuditorResult.IsDataDirty)
                return;
                
            string json = JsonWrapper.ToJson(SpriteAuditorResult);
            EditorPrefs.SetString(ATLAS_AUDITOR_STORAGE_KEY, json);
            SpriteAuditorResult.SetDataDirty(false);
        }
        
        private void ClearCache()
        {
            EditorPrefs.DeleteKey(ATLAS_AUDITOR_STORAGE_KEY);
            cachedSpriteAuditorResult = new SpriteAuditorResult();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false);

            DrawSettings();
            DrawResults();
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawResults()
        {
            if (SpriteAuditorResult == null)
                return;

            SpriteAuditorResult.AssignReferences();

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Results", EditorStyles.toolbarDropDown);
            EditorGUILayout.Space();

            visualizationType =
                (VisualizationType) GUILayout.SelectionGrid((int) visualizationType, VISUALIZATION_NAMES, 2,
                    EditorStyles.radioButton);


            switch (visualizationType)
            {
                case VisualizationType.Scene:
                    DrawResultsByScene();
                    break;
                case VisualizationType.Atlas:
                    DrawResultsByAtlas();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawResultsByAtlas()
        {
            EditorGUILayout.BeginVertical("Box");

            if (SpriteAuditorResult.AtlasToUsedSprites.Count > 0)
            {
                if (DrawStringFoldout("Used Atlases", ATLAS_VIEW_KEY))
                {
                    EditorGUI.indentLevel++;
                    foreach (var atlasToUsedSprites in SpriteAuditorResult.AtlasToUsedSprites)
                    {
                        EditorGUILayout.BeginVertical("Box");

                        if (DrawObjectFoldout(atlasToUsedSprites.Key, $"{ATLAS_VIEW_KEY}_{atlasToUsedSprites.Key.name}"))
                        {
                            if (atlasToUsedSprites.Value.Count > 0)
                            {
                                EditorGUI.indentLevel++;

                                EditorGUILayout.BeginVertical("Box");

                                if (DrawStringFoldout("Used Sprites", $"{ATLAS_VIEW_KEY}_USED_SPRITES"))
                                {
                                    EditorGUI.indentLevel++;
                                    foreach (Sprite sprite in atlasToUsedSprites.Value)
                                    {
                                        DrawSpriteField(sprite, null, SpriteDetails.All,
                                            $"{ATLAS_VIEW_KEY}_{atlasToUsedSprites.Key.name}");
                                    }
                                    EditorGUI.indentLevel--;
                                }
                                EditorGUI.indentLevel--;
                                EditorGUILayout.EndVertical();
                            }

                            if (SpriteAuditorResult.AtlasToNotUsedSprites[atlasToUsedSprites.Key].Count > 0)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.BeginVertical("Box");
                                if (DrawStringFoldout("Not used sprites", $"{ATLAS_VIEW_KEY}_NOT_USED_SPRITES"))
                                {
                                    EditorGUI.indentLevel++;
                                    foreach (Sprite sprite in SpriteAuditorResult.AtlasToNotUsedSprites[atlasToUsedSprites.Key])
                                    {
                                        DrawSpriteField(sprite, null, SpriteDetails.None,
                                            $"{ATLAS_VIEW_KEY}_{atlasToUsedSprites.Key.name}");
                                    }
                                    EditorGUI.indentLevel--;
                                }
                                EditorGUILayout.EndVertical();
                                EditorGUI.indentLevel--;
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private bool DrawStringFoldout(string label, string foldoutKey)
        {
            if(!keyToFoldout.ContainsKey(foldoutKey))
                keyToFoldout.Add(foldoutKey, false);

            keyToFoldout[foldoutKey] = EditorGUILayout.Foldout(keyToFoldout[foldoutKey], label, true,
                EditorStyles.foldout);
            return keyToFoldout[foldoutKey];
        }

        private void DrawResultsByScene()
        {
            EditorGUILayout.BeginVertical("Box");
            
            foreach (var sceneGUIDtoSceneAsset in SpriteAuditorResult.SceneGUIDToSceneAssetCache)
            {
                SceneAsset sceneAsset = sceneGUIDtoSceneAsset.Value;
                EditorGUILayout.LabelField(sceneAsset.name, EditorStyles.toolbarDropDown);

                if (SpriteAuditorResult.SceneToSingleSprites[sceneAsset].Count > 0)
                {
                    EditorGUILayout.BeginVertical("Box");
                    showSpritesWithoutAtlas = EditorGUILayout.Foldout(showSpritesWithoutAtlas, "Sprites without atlas",
                        EditorStyles.foldout);
                    if (showSpritesWithoutAtlas)
                    {
                        EditorGUI.indentLevel++;
                        foreach (Sprite sprite in SpriteAuditorResult.SceneToSingleSprites[sceneAsset])
                        {
                            DrawSpriteField(sprite, sceneAsset, SpriteDetails.All, sceneAsset.name);
                        }
                        EditorGUI.indentLevel--;

                    }
                    EditorGUILayout.EndVertical();
                }

                foreach (var valuePair in SpriteAuditorResult.SceneToSpriteAtlasToSprites[sceneAsset])
                {
                    EditorGUILayout.BeginVertical("Box");

                    if (DrawObjectFoldout(valuePair.Key, $"{SCENE_VIEW_KEY}_{valuePair.Key}"))
                    {
                        EditorGUI.indentLevel++;
                        foreach (Sprite sprite in SpriteAuditorResult.SceneToSpriteAtlasToSprites[sceneAsset][valuePair.Key])
                        {
                            DrawSpriteField(sprite, sceneAsset, SpriteDetails.All, valuePair.Key.name);
                        }
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.EndVertical();

                }
            }
            EditorGUILayout.EndVertical();
        }

        private bool DrawObjectFoldout<T>(T targetObject, string foldoutKey, bool showFoldout = true) where T:Object
        {
            if (!keyToFoldout.ContainsKey(foldoutKey))
                keyToFoldout.Add(foldoutKey, false);
            
            EditorGUILayout.BeginHorizontal();

            if (showFoldout)
            {
                GUIStyle style = new GUIStyle(EditorStyles.foldout)
                {
                    fixedWidth = 5
                };
                    
                keyToFoldout[foldoutKey] = EditorGUILayout.Foldout(keyToFoldout[foldoutKey], "", true, style);
                GUILayout.Space(-34);
            }
                    
            EditorGUILayout.ObjectField(targetObject, typeof(T), false);
            EditorGUILayout.EndHorizontal();

            return keyToFoldout[foldoutKey];
        }

        private void DrawSpriteField(Sprite sprite, SceneAsset sceneAsset = null,
            SpriteDetails details = SpriteDetails.All, string foldoutKey = "")
        {
            EditorGUILayout.BeginVertical("Box");

            if (DrawObjectFoldout(sprite, $"{foldoutKey}_{sprite.name}", !details.HasFlag(SpriteDetails.None)))
            {
                EditorGUI.indentLevel++;

                if (details.HasFlag(SpriteDetails.UsageCount))
                    DrawSpriteUsageCount(sprite);

                if (details.HasFlag(SpriteDetails.SizeDetails))
                    DrawSpriteSizeDetails(sprite);    

                if (details.HasFlag(SpriteDetails.ReferencesPath))
                    DrawSpriteReferencesPath(sceneAsset, sprite);

                if (details.HasFlag(SpriteDetails.SceneReferences))
                    DrawSpriteSceneReferences(sprite);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSpriteSceneReferences(Sprite sprite)
        {
            EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (var scene in SpriteAuditorResult.SpriteToScenes[sprite])
            {
                EditorGUILayout.ObjectField(scene, typeof(SceneAsset), false);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSpriteReferencesPath(SceneAsset sceneAsset, Sprite sprite)
        {
            EditorGUILayout.LabelField("Usages", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (string usePath in SpriteAuditorResult.SpriteToUseTransformPath[sprite])
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(usePath);
                if (GUILayout.Button("Select", EditorStyles.miniButton))
                {
                    TrySelectObjectAtPath(sceneAsset, usePath);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSpriteUsageCount(Sprite sprite)
        {
            EditorGUILayout.LabelField($"Total Usages Found", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"{SpriteAuditorResult.GetSpriteUseCount(sprite)}");
            EditorGUI.indentLevel--;
        }

        private void DrawSpriteSizeDetails(Sprite sprite)
        {
            EditorGUILayout.LabelField("Max Use Size", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            Vector3 spriteMaxUseSize = SpriteAuditorResult.GetSpriteMaxUseSize(sprite);
            EditorGUILayout.LabelField(
                $"Width: {Mathf.RoundToInt(spriteMaxUseSize.x)} Height: {Mathf.RoundToInt(spriteMaxUseSize.y)}");

            Vector3 sizeDifference = new Vector3(spriteMaxUseSize.x - sprite.rect.size.x,
                spriteMaxUseSize.y - sprite.rect.size.y, 0);

            float differenceMagnitude = sizeDifference.magnitude / sprite.rect.size.magnitude;

            if (Mathf.Abs(differenceMagnitude) > spriteUsageSizeThreshold)
            {
                if (spriteMaxUseSize.sqrMagnitude > sprite.rect.size.sqrMagnitude)
                {
                    EditorGUILayout.HelpBox(
                        $"Sprite used with a size {differenceMagnitude:P} times bigger than imported sprite size," +
                        $" you may consider resizing it up",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"Sprite used with a size {differenceMagnitude:P} times smaller than imported sprite size," +
                        $" you may consider resizing it down",
                        MessageType.Warning);
                }
            }
 
            EditorGUI.indentLevel--;
        }

        private void TrySelectObjectAtPath(SceneAsset sceneAsset, string usePath)
        {
            if (!Application.isPlaying)
            {
                if (sceneAsset != null)
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(sceneAsset), OpenSceneMode.Additive);
            }
            
            GameObject gameObject = GameObject.Find(usePath);
            if (gameObject == null)
                return;
            
            Selection.SetActiveObjectWithContext(gameObject, this);
        }

        private void DrawSettings()
        {
            EditorGUI.BeginDisabledGroup(isRecording);
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Settings", EditorStyles.toolbarDropDown);
            EditorGUILayout.Space();

            recordOnPlay = EditorGUILayout.ToggleLeft("Record on play", recordOnPlay);

            spriteUsageSizeThreshold = EditorGUILayout.Slider("Size Difference Threshold", spriteUsageSizeThreshold, 0, 1);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!EditorPrefs.HasKey(ATLAS_AUDITOR_STORAGE_KEY));
            if (GUILayout.Button("Clear Cache", EditorStyles.toolbarButton))
                ClearCache();
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Refresh References", EditorStyles.toolbarButton))
                SpriteAuditorResult.SetReferencesDirty(true);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }


        private void StopRecording()
        {
            if (!isRecording)
                return;
            
            if (spriteAuditorForwarder != null)
                Destroy(spriteAuditorForwarder.gameObject);
            
            isRecording = false;
            SaveAtlasResult();
        }

        private void StartRecording()
        {
            if(isRecording)
                return;
            
            isRecording = true;

            spriteFinder.SetResult(SpriteAuditorResult);
            GameObject atlasDetectingGameObject = new GameObject("Atlas Detecting");
            
            spriteAuditorForwarder = atlasDetectingGameObject.AddComponent<SpriteAuditorForwarder>();
            spriteAuditorForwarder.SetListener(spriteFinder);
            DontDestroyOnLoad(atlasDetectingGameObject.gameObject);
        }
    }
}