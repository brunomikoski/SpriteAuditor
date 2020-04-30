using System;
using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.SpriteAuditor.Serialization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace BrunoMikoski.SpriteAuditor
{
    public class SpriteAuditorWindow : EditorWindow
    {
        private const string ATLAS_AUDITOR_STORAGE_KEY = "ATLAS_AUDITOR_STORAGE_KEY";
        private static string[] VISUALIZATION_NAMES = {"Scene View", "Atlas View"};
        
        private bool isRecording;
        
        private SpriteFinder spriteFinder = new SpriteFinder();

        [SerializeField] 
        private SceneAsset[] ignoredScenes;
        
        [NonSerialized]
        private SpriteDatabase cachedSpriteDatabase;
        private SpriteDatabase SpriteDatabase
        {
            get
            {
                if (cachedSpriteDatabase == null)
                    LoadOrCreateAtlasResult();
                return cachedSpriteDatabase;
            }
        }

        private ResultViewDataBase[] resultViews;

        private Dictionary<string, bool> keyToFoldout = new Dictionary<string, bool>();

        private bool dontDestroyOnLoadFoldout;
        private bool scenesFoldout;
        private bool atlasesFoldout;

        private Vector2 scrollPosition = Vector2.zero;

        private bool recordOnPlay = true;
        private VisualizationType visualizationType = VisualizationType.Scene;
        private SpriteAuditorForwarder spriteAuditorForwarder;
        private float spriteUsageSizeThreshold = 0.25f;
        
        private SerializedObject spriteAuditorWindowSerializedObject;

        [MenuItem("Tools/Sprite Auditor")]
        public static void OpenWindow()
        {
            SpriteAuditorWindow window = GetWindow<SpriteAuditorWindow>("Atlas Auditor");
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            spriteAuditorWindowSerializedObject = new SerializedObject(this);
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
                if (isRecording)
                {
                    StopRecording();
                    SpriteDatabase.GenerateResults();
                }
            }
        }

        private void LoadOrCreateAtlasResult()
        {
            string storedJson = EditorPrefs.GetString(ATLAS_AUDITOR_STORAGE_KEY, string.Empty);
            if (!string.IsNullOrEmpty(storedJson))
            {
                cachedSpriteDatabase = new SpriteDatabase();
                JsonWrapper.FromJson(storedJson, ref cachedSpriteDatabase);
            }
            else
            {
                cachedSpriteDatabase = new SpriteDatabase();
            }
        }

        private void SaveAtlasResult()
        {
            if (!SpriteDatabase.IsSaveDataDirty)
                return;
                
            string json = JsonWrapper.ToJson(SpriteDatabase);
            EditorPrefs.SetString(ATLAS_AUDITOR_STORAGE_KEY, json);
            SpriteDatabase.SetDataDirty(false);
        }
        
        private void ClearCache()
        {
            EditorPrefs.DeleteKey(ATLAS_AUDITOR_STORAGE_KEY);
            cachedSpriteDatabase = new SpriteDatabase();
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
            if (SpriteDatabase == null)
                return;

            SpriteDatabase.GenerateResults();

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Results", EditorStyles.toolbarDropDown);
            EditorGUILayout.Space();

            visualizationType =
                (VisualizationType) GUILayout.SelectionGrid((int) visualizationType, VISUALIZATION_NAMES, 2,
                    EditorStyles.radioButton);
            
            EditorGUILayout.EndVertical();

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

            if (GUILayout.Button("Refresh Results"))
            {
                resultViews[(int) visualizationType].GenerateResults(SpriteDatabase);
            }
        }

        private void DrawResultsByAtlas()
        {
            if (SpriteDatabase.AtlasToUsedSprites.Count > 0)
            {
                EditorGUILayout.BeginVertical("Box");

                if (DrawStringFoldout("Used Atlases", VisualizationType.Atlas.ToString()))
                {
                    EditorGUI.indentLevel++;
                    foreach (var atlasToUsedSprites in SpriteDatabase.AtlasToUsedSprites)
                    {
                        EditorGUILayout.BeginVertical("Box");

                        if (DrawObjectFoldout(atlasToUsedSprites.Key, $"{VisualizationType.Atlas.ToString()}_{atlasToUsedSprites.Key.name}"))
                        {
                            if (atlasToUsedSprites.Value.Count > 0)
                            {
                                EditorGUI.indentLevel++;

                                EditorGUILayout.BeginVertical("Box");

                                if (DrawStringFoldout("Used Sprites", $"{VisualizationType.Atlas.ToString()}_USED_SPRITES"))
                                {
                                    EditorGUI.indentLevel++;
                                    foreach (Sprite sprite in atlasToUsedSprites.Value)
                                    {
                                        DrawSpriteField(sprite, atlasToUsedSprites.Key,null, SpriteDrawDetails.All,
                                            $"{VisualizationType.Atlas.ToString()}_{atlasToUsedSprites.Key.name}");
                                    }
                                    EditorGUI.indentLevel--;
                                }
                                EditorGUI.indentLevel--;
                                EditorGUILayout.EndVertical();
                            }

                            if (SpriteDatabase.AtlasToNotUsedSprites[atlasToUsedSprites.Key].Count > 0)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.BeginVertical("Box");
                                if (DrawStringFoldout("Not used sprites", $"{VisualizationType.Atlas.ToString()}_NOT_USED_SPRITES"))
                                {
                                    EditorGUI.indentLevel++;
                                    foreach (Sprite sprite in SpriteDatabase.AtlasToNotUsedSprites[atlasToUsedSprites.Key])
                                    {
                                        DrawSpriteField(sprite, atlasToUsedSprites.Key,null, SpriteDrawDetails.None,
                                            $"{VisualizationType.Atlas.ToString()}_{atlasToUsedSprites.Key.name}");
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
                EditorGUILayout.EndVertical();
            }
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
            SceneViewResultData sceneViewResult = (SceneViewResultData) resultViews[(int) VisualizationType.Scene];

            EditorGUILayout.BeginVertical("Box");


            for (int i = 0; i < sceneViewResult.SceneAssets.Length; i++)
            {
                SceneAsset sceneAsset = sceneViewResult.SceneAssets[i];
                
                EditorGUILayout.BeginVertical("Box");
                if (ignoredScenes.Contains(sceneAsset))
                    continue;

                if (DrawObjectFoldout(sceneAsset, sceneAsset.name))
                {
                    EditorGUI.indentLevel++;
                    if (sceneViewResult.sceneToSingleSprites[sceneAsset].Length > 0)
                    {
                        EditorGUILayout.BeginVertical("Box");
                        
                        if(DrawStringFoldout("Sprites Without Atlas", "SceneViewSpritesWithoutAtlas"))
                        {
                            EditorGUI.indentLevel++;
                            foreach (SpriteData spriteData in sceneViewResult.sceneToSingleSprites[sceneAsset])
                            {
                                DrawSpriteDataField(spriteData, SpriteDrawDetails.All);
                            }

                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.EndVertical();
                    }

                    foreach (var atlasToUSedSprites in sceneViewResult.sceneToAtlasToUsedSprites[sceneAsset])
                    {
                        EditorGUILayout.BeginVertical("Box");

                        if (DrawObjectFoldout(atlasToUSedSprites.Key, $"{VisualizationType.Scene.ToString()}_{atlasToUSedSprites.Key}"))
                        {
                            EditorGUI.indentLevel++;
                            foreach (SpriteData spriteData in sceneViewResult.sceneToAtlasToUsedSprites[sceneAsset][atlasToUSedSprites.Key])
                            {
                                DrawSpriteDataField(spriteData, SpriteDrawDetails.All);
                            }

                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.EndVertical();
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
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

        private void DrawSpriteDataField(SpriteData spriteData, SpriteDrawDetails drawDetails)
        {
            if (DrawObjectFoldout(spriteData.Sprite, spriteData.Sprite.name, !drawDetails.HasFlag(SpriteDrawDetails.None)))
            {
                EditorGUI.indentLevel++;

                if (drawDetails.HasFlag(SpriteDrawDetails.UsageCount))
                    DrawSpriteUsageCount(spriteData);

                if (drawDetails.HasFlag(SpriteDrawDetails.SizeDetails))
                    DrawSpriteSizeDetails(spriteData);

                if (drawDetails.HasFlag(SpriteDrawDetails.ReferencesPath))
                    DrawSpriteReferencesPath(spriteData);

                if (drawDetails.HasFlag(SpriteDrawDetails.SceneReferences))
                    DrawSpriteSceneReferences(spriteData);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawSpriteField(Sprite sprite, SpriteAtlas atlas = null, SceneAsset sceneAsset = null,
            SpriteDrawDetails drawDetails = SpriteDrawDetails.All, string foldoutKey = "")
        {
            EditorGUILayout.BeginVertical("Box");

            if (DrawObjectFoldout(sprite, $"{foldoutKey}_{sprite.name}", !drawDetails.HasFlag(SpriteDrawDetails.None)))
            {
                EditorGUI.indentLevel++;

                if (drawDetails.HasFlag(SpriteDrawDetails.UsageCount))
                    DrawSpriteUsageCount(sprite);

                if (drawDetails.HasFlag(SpriteDrawDetails.SizeDetails))
                {
                    float scale = 1.0f;
                    if (atlas != null)
                        scale = SpriteDatabase.AtlasToScale[atlas];
                    DrawSpriteSizeDetails(sprite, scale);
                }    

                if (drawDetails.HasFlag(SpriteDrawDetails.ReferencesPath))
                    DrawSpriteReferencesPath(sceneAsset, sprite);

                if (drawDetails.HasFlag(SpriteDrawDetails.SceneReferences))
                    DrawSpriteSceneReferences(sprite);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSpriteSceneReferences(SpriteData spriteData)
        {
            EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (SceneAsset sceneAsset in spriteData.SceneAssets)
            {
                EditorGUILayout.ObjectField(sceneAsset, typeof(SceneAsset), false);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSpriteSceneReferences(Sprite sprite)
        {
            EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (var scene in SpriteDatabase.SpriteToScenes[sprite])
            {
                EditorGUILayout.ObjectField(scene, typeof(SceneAsset), false);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSpriteReferencesPath(SpriteData spriteData)
        {
            EditorGUILayout.LabelField("Usages", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            for (int i = 0; i < spriteData.Usages.Count; i++)
            {
                SpriteUseData spriteUseData = spriteData.Usages[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(spriteUseData.FirstPath);
                
                if (GUILayout.Button("Select", EditorStyles.miniButton))
                    TrySelectObjectAtPath(spriteUseData.FirstPath);
                
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

       

        private void DrawSpriteReferencesPath(SceneAsset sceneAsset, Sprite sprite)
        {
            EditorGUILayout.LabelField("Usages", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (string usePath in SpriteDatabase.SpriteToUseTransformPath[sprite])
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
            EditorGUILayout.LabelField($"{SpriteDatabase.GetSpriteUseCount(sprite)}");
            EditorGUI.indentLevel--;
        }
        
        private void DrawSpriteUsageCount(SpriteData spriteData)
        {
            EditorGUILayout.LabelField($"Total Usages Found", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"{spriteData.Usages.Count}");
            EditorGUI.indentLevel--;
        }

        private void DrawSpriteSizeDetails(SpriteData spriteData)
        {
            EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            Vector3 minUseSize = spriteData.MinimumUsageSize;
            if (minUseSize != Vector3.zero)
            {
                EditorGUILayout.LabelField(
                    $"Minimum Use Size Width: {Mathf.RoundToInt(minUseSize.x)} Height: {Mathf.RoundToInt(minUseSize.y)}");
            }
            
            Vector3 maxUseSize = spriteData.MaximumUsageSize;
            if (maxUseSize != Vector3.zero)
            {
                EditorGUILayout.LabelField(
                    $"Max Use Size Width: {Mathf.RoundToInt(maxUseSize.x)} Height: {Mathf.RoundToInt(maxUseSize.y)}");
            }
            
            Vector3 avgUseSize = spriteData.AverageUsageSize;
            if (avgUseSize != Vector3.zero)
            {
                EditorGUILayout.LabelField(
                    $"Average Size Width: {Mathf.RoundToInt(avgUseSize.x)} Height: {Mathf.RoundToInt(avgUseSize.y)}");
            }

            Vector2 spriteSize = spriteData.SpriteSize;
            EditorGUILayout.LabelField(
                $"Sprite Rect Size Width: {Mathf.RoundToInt(spriteSize.x)} Height: {Mathf.RoundToInt(spriteSize.y)}");

            
            // Vector3 sizeDifference = new Vector3(avgUseSize.x - spriteSize.x,
            //     avgUseSize.y - spriteSize.y, 0);
            //
            // float differenceMagnitude = sizeDifference.magnitude / spriteSize.magnitude;
            //
            // if (Mathf.Abs(differenceMagnitude) > spriteUsageSizeThreshold)
            // {
            //     if (avgUseSize.sqrMagnitude > spriteSize.sqrMagnitude)
            //     {
            //         EditorGUILayout.HelpBox(
            //             $"Sprite used with a size {differenceMagnitude:P} times bigger than imported sprite size," +
            //             $" you may consider resizing it up",
            //             MessageType.Warning);
            //     }
            //     else
            //     {
            //         EditorGUILayout.HelpBox(
            //             $"Sprite used with a size {differenceMagnitude:P} times smaller than imported sprite size," +
            //             $" you may consider resizing it down",
            //             MessageType.Warning);
            //     }
            // }
 
            EditorGUI.indentLevel--;
        }

        private void DrawSpriteSizeDetails(Sprite sprite, float atlasScale = 1)
        {
            EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            Vector3 spriteMaxUseSize = SpriteDatabase.GetSpriteMaxUseSize(sprite);
            EditorGUILayout.LabelField(
                $"Max Use Size Width: {Mathf.RoundToInt(spriteMaxUseSize.x)} Height: {Mathf.RoundToInt(spriteMaxUseSize.y)}");

            Vector2 spriteSize = sprite.rect.size;
            spriteSize = spriteSize * atlasScale;
            
            EditorGUILayout.LabelField(
                $"Sprite Size Width: {Mathf.RoundToInt(spriteSize.x)} Height: {Mathf.RoundToInt(spriteSize.y)}");

            
            Vector3 sizeDifference = new Vector3(spriteMaxUseSize.x - spriteSize.x,
                spriteMaxUseSize.y - spriteSize.y, 0);

            float differenceMagnitude = sizeDifference.magnitude / spriteSize.magnitude;

            if (Mathf.Abs(differenceMagnitude) > spriteUsageSizeThreshold)
            {
                if (spriteMaxUseSize.sqrMagnitude > spriteSize.sqrMagnitude)
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

        
        
        private void TrySelectObjectAtPath(string selectionPath)
        {
            string[] paths = selectionPath.Split(':');
            if (paths.Length < 2)
                return;
            if (!Application.isPlaying)
            {
                EditorSceneManager.OpenScene(paths[0], OpenSceneMode.Additive);
            }
            GameObject gameObject = GameObject.Find(paths[1]);
            if (gameObject == null)
                return;
            
            Selection.SetActiveObjectWithContext(gameObject, this);
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

            recordOnPlay = EditorGUILayout.Toggle("Record on play", recordOnPlay);

            spriteUsageSizeThreshold = EditorGUILayout.Slider("Size Difference Threshold", spriteUsageSizeThreshold, 0, 1);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!EditorPrefs.HasKey(ATLAS_AUDITOR_STORAGE_KEY));
            if (GUILayout.Button("Clear Cache", EditorStyles.toolbarButton))
                ClearCache();
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Refresh References", EditorStyles.toolbarButton))
                SpriteDatabase.SetReferencesDirty(true);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Caching Tools", EditorStyles.toolbarDropDown);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Know Atlas", EditorStyles.toolbarButton))
                SpriteDatabase.ClearAtlasesCache();
            
            if (GUILayout.Button("Clear Maximum Size References", EditorStyles.toolbarButton))
                SpriteDatabase.ClearAllSpritesKnowSizes();

            if (GUILayout.Button("Pack Atlases", EditorStyles.toolbarButton))
                SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
            
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

            spriteFinder.SetResult(SpriteDatabase);
            GameObject spriteAuditorGameObject = new GameObject("Sprite Auditor Forwarder");
            
            spriteAuditorForwarder = spriteAuditorGameObject.AddComponent<SpriteAuditorForwarder>();
            spriteAuditorForwarder.SetListener(spriteFinder);
            DontDestroyOnLoad(spriteAuditorForwarder.gameObject);
        }
    }
}
