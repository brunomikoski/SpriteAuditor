using System;
using System.Collections.Generic;
using BrunoMikoski.AtlasAudior.Serialization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Sprites;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;
using SpriteAtlasExtensions = UnityEditor.U2D.SpriteAtlasExtensions;

namespace BrunoMikoski.AtlasAudior
{
    public class AtlasAuditorWindow : EditorWindow
    {
        private const string ATLAS_AUDITOR_STORAGE_KEY = "ATLAS_AUDITOR_STORAGE_KEY";
        private static string[] VISUALIZATION_NAMES = {"Scene View", "Atlas View"};

        private enum VisualizationType
        {
            Scene,
            Atlas
        }
        
        private bool isRecording;

        
        private AtlasAuditorSpriteDetector atlasAuditorSpriteDetector = new AtlasAuditorSpriteDetector();
        
        [NonSerialized]
        private AtlasAuditorResult cachedAtlasAuditorResult;
        private AtlasAuditorResult AtlasAuditorResult
        {
            get
            {
                if (cachedAtlasAuditorResult == null)
                    LoadOrCreateAtlasResult();
                return cachedAtlasAuditorResult;
            }
        }

        private Dictionary<SceneAsset, bool> sceneToFoldout = new Dictionary<SceneAsset, bool>();
        private Dictionary<SceneAsset, bool> sceneToSingleSprites = new Dictionary<SceneAsset, bool>();
        private Dictionary<SpriteAtlas, bool> atlasToFoldout = new Dictionary<SpriteAtlas, bool>();
        private Dictionary<Sprite, bool> spriteToFoldout = new Dictionary<Sprite, bool>();

        private bool dontDestroyOnLoadFoldout;
        private bool scenesFoldout;
        private bool atlasesFoldout;

        private Vector2 scrollPosition = Vector2.zero;

        private bool recordOnPlay = true;
        private VisualizationType visualizationType = VisualizationType.Scene;
        private AtlasAuditorForwarder atlasAuditorForwarder;
        private bool showSpritesWithoutAtlas;
        private float spriteUsageSizeThreshold = 0.25f;

        [MenuItem("Tools/Atlas Auditor")]
        public static void OpenWindow()
        {
            AtlasAuditorWindow window = GetWindow<AtlasAuditorWindow>("Atlas Auditor");
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
            AtlasAuditorResult?.SetReferencesDirty(true);
        }

        private void LoadOrCreateAtlasResult()
        {
            string storedJson = EditorPrefs.GetString(ATLAS_AUDITOR_STORAGE_KEY, string.Empty);
            if (!string.IsNullOrEmpty(storedJson))
            {
                cachedAtlasAuditorResult = new AtlasAuditorResult();
                JsonWrapper.FromJson(storedJson, ref cachedAtlasAuditorResult);
            }
            else
            {
                cachedAtlasAuditorResult = new AtlasAuditorResult();
            }
        }

        private void SaveAtlasResult()
        {
            if (!AtlasAuditorResult.IsDataDirty)
                return;
                
            string json = JsonWrapper.ToJson(AtlasAuditorResult);
            EditorPrefs.SetString(ATLAS_AUDITOR_STORAGE_KEY, json);
            AtlasAuditorResult.SetDataDirty(false);
        }
        
        private void ClearCache()
        {
            EditorPrefs.DeleteKey(ATLAS_AUDITOR_STORAGE_KEY);
            cachedAtlasAuditorResult = new AtlasAuditorResult();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false);

            DrawSettings();
            
            DrawResults();
            
            // DrawControls();
            // DrawLastResults();
            EditorGUILayout.EndScrollView();
        }

        private void DrawResults()
        {
            if (AtlasAuditorResult == null)
                return;

            AtlasAuditorResult.AssignReferences();

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

            foreach (var atlasToSprites in AtlasAuditorResult.AtlasToAllSprites)
            {
                if (DrawObjectFoldout(atlasToSprites.Key, atlasToFoldout))
                {
                    foreach (Sprite sprite in atlasToSprites.Value)
                    {
                        DrawSpriteField(sprite);
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawResultsByScene()
        {
            EditorGUILayout.BeginVertical("Box");
            
            foreach (var sceneGUIDtoSceneAsset in AtlasAuditorResult.SceneGUIDToSceneAssetCache)
            {
                SceneAsset sceneAsset = sceneGUIDtoSceneAsset.Value;
                EditorGUILayout.LabelField(sceneAsset.name, EditorStyles.toolbarDropDown);

                if (AtlasAuditorResult.SceneToSingleSprites[sceneAsset].Count > 0)
                {
                    EditorGUILayout.BeginVertical("Box");
                    showSpritesWithoutAtlas = EditorGUILayout.Foldout(showSpritesWithoutAtlas, "Sprites without atlas",
                        EditorStyles.foldout);
                    if (showSpritesWithoutAtlas)
                    {
                        EditorGUI.indentLevel++;
                        foreach (Sprite sprite in AtlasAuditorResult.SceneToSingleSprites[sceneAsset])
                        {
                            DrawSpriteField(sprite, sceneAsset);
                        }
                        EditorGUI.indentLevel--;

                    }
                    EditorGUILayout.EndVertical();
                }

                foreach (var valuePair in AtlasAuditorResult.SceneToSpriteAtlasToSprites[sceneAsset])
                {
                    EditorGUILayout.BeginVertical("Box");

                    if (DrawObjectFoldout(valuePair.Key, atlasToFoldout))
                    {
                        EditorGUI.indentLevel++;
                        foreach (Sprite sprite in AtlasAuditorResult.SceneToSpriteAtlasToSprites[sceneAsset][valuePair.Key])
                        {
                            DrawSpriteField(sprite, sceneAsset);
                        }
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.EndVertical();

                }
            }
            EditorGUILayout.EndVertical();
        }

        private bool DrawObjectFoldout<T>(T targetObject, Dictionary<T, bool> foldoutDictionary) where T:Object
        {
            if (!foldoutDictionary.ContainsKey(targetObject))
                foldoutDictionary.Add(targetObject, false);
            
            EditorGUILayout.BeginHorizontal();
                 
            GUIStyle style = new GUIStyle(EditorStyles.foldout)
            {
                fixedWidth = 5
            };
                    
            foldoutDictionary[targetObject] = EditorGUILayout.Foldout(foldoutDictionary[targetObject], "", style);
            GUILayout.Space(-34);
                    
            EditorGUILayout.ObjectField(targetObject, typeof(T), false);
            EditorGUILayout.EndHorizontal();


            return foldoutDictionary[targetObject];
        }

        private void DrawSpriteField(Sprite sprite, SceneAsset sceneAsset = null)
        {
            EditorGUILayout.BeginVertical("Box");

            if (DrawObjectFoldout(sprite, spriteToFoldout))
            {
                EditorGUI.indentLevel++;

                DrawSpriteUsageCount(sprite);

                DrawSpriteSizeDetails(sprite);

                DrawSpriteReferencesPath(sceneAsset, sprite);

                DrawSpriteSceneReferences(sprite);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

        }

        private void DrawSpriteSceneReferences(Sprite sprite)
        {
            EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (var scene in AtlasAuditorResult.SpriteToScenes[sprite])
            {
                EditorGUILayout.ObjectField(scene, typeof(SceneAsset), false);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSpriteReferencesPath(SceneAsset sceneAsset, Sprite sprite)
        {
            EditorGUILayout.LabelField("Usages", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (string usePath in AtlasAuditorResult.SpriteToUseTransformPath[sprite])
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
            EditorGUILayout.LabelField($"{AtlasAuditorResult.GetSpriteUseCount(sprite)}");
            EditorGUI.indentLevel--;
        }

        private void DrawSpriteSizeDetails(Sprite sprite)
        {
            EditorGUILayout.LabelField("Max Use Size", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            Vector3 spriteMaxUseSize = AtlasAuditorResult.GetSpriteMaxUseSize(sprite);
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
                AtlasAuditorResult.SetReferencesDirty(true);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }

        // private void DrawLastResults()
        // {
        //     if (AtlasAuditorResult == null)
        //         return;
        //
        //     AtlasAuditorResult.AssignReferences();
        //     EditorGUI.BeginDisabledGroup(true);
        //
        //     scenesFoldout = EditorGUILayout.Foldout(scenesFoldout, "Scenes");
        //     if (scenesFoldout)
        //     {
        //         EditorGUI.indentLevel++;
        //         foreach (SceneAsset sceneAsset in AtlasAuditorResult.KnowScenes)
        //         {
        //             DrawSceneResults(sceneAsset);
        //         }
        //
        //         DrawDontDestroyOnLoadResults();
        //         EditorGUI.indentLevel--;
        //     }
        //
        //     atlasesFoldout = EditorGUILayout.Foldout(atlasesFoldout, "Altases");
        //     if (atlasesFoldout)
        //     {                
        //         EditorGUI.indentLevel++;
        //
        //         foreach (var atlasToSprite in AtlasAuditorResult.AtlasToSprites)
        //         {
        //             if (atlasToSprite.Key.isVariant)
        //                 continue;
        //             
        //             DrawAtlas(atlasToSprite.Key);
        //         }
        //         EditorGUI.indentLevel--;
        //     }
        //         
        //     EditorGUI.EndDisabledGroup();
        // }

        private void DrawAtlas(SpriteAtlas targetAtlas)
        {
            if (!atlasToFoldout.ContainsKey(targetAtlas))
                atlasToFoldout.Add(targetAtlas, false);
                    
            atlasToFoldout[targetAtlas] = EditorGUILayout.Foldout(atlasToFoldout[targetAtlas], targetAtlas.name);
            if (atlasToFoldout[targetAtlas])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.ObjectField("Atlas", targetAtlas, typeof(SpriteAtlas), false);

                List<Sprite> usedSprites = new List<Sprite>();
                List<Sprite> unusedSprites = new List<Sprite>();

                foreach (Sprite sprite in AtlasAuditorResult.AtlasToAllSprites[targetAtlas])
                {
                    if (AtlasAuditorResult.TryGetSpriteSceneUsages(sprite, out HashSet<SceneAsset> scenes))
                    {
                        usedSprites.Add(sprite);
                    }
                    else
                    {
                        unusedSprites.Add(sprite);
                    }
                }
                
                EditorGUILayout.LabelField("Sprites used so far", EditorStyles.boldLabel);
                for (int i = 0; i < usedSprites.Count; i++)
                {
                    Sprite usedSprite = usedSprites[i];
                    EditorGUILayout.ObjectField(usedSprite.name, usedSprite, typeof(Object), false);
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Sprites never used", EditorStyles.boldLabel);
                for (int i = 0; i < unusedSprites.Count; i++)
                {
                    Sprite usedSprite = unusedSprites[i];
                    EditorGUILayout.ObjectField(usedSprite.name, usedSprite, typeof(Object), false);
                }

                bool wasGUIEnabled = GUI.enabled;
                GUI.enabled = true;
                if (GUILayout.Button("Select all unused"))
                {
                    Selection.objects = unusedSprites.ToArray();
                }
                GUI.enabled = wasGUIEnabled;
                
                EditorGUI.indentLevel--;
            }
        }


        private void StopRecording()
        {
            if (!isRecording)
                return;
            
            if (atlasAuditorForwarder != null)
                Destroy(atlasAuditorForwarder.gameObject);
            
            isRecording = false;
            SaveAtlasResult();
        }

        private void StartRecording()
        {
            if(isRecording)
                return;
            
            isRecording = true;

            atlasAuditorSpriteDetector.SetResult(AtlasAuditorResult);
            GameObject atlasDetectingGameObject = new GameObject("Atlas Detecting");
            
            atlasAuditorForwarder = atlasDetectingGameObject.AddComponent<AtlasAuditorForwarder>();
            atlasAuditorForwarder.SetListener(atlasAuditorSpriteDetector);
            DontDestroyOnLoad(atlasDetectingGameObject.gameObject);
        }
    }
}
