using System;
using BrunoMikoski.SpriteAuditor.Serialization;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

namespace BrunoMikoski.SpriteAuditor
{
    public class SpriteAuditorWindow : EditorWindow
    {
        private const string ATLAS_AUDITOR_STORAGE_KEY = "ATLAS_AUDITOR_STORAGE_KEY";
        private static string[] VISUALIZATION_NAMES = {"Scene View", "Atlas View"};
        
        private bool isRecording;
        
        private SpriteFinder spriteFinder = new SpriteFinder();

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

        private bool dontDestroyOnLoadFoldout;
        private bool scenesFoldout;
        private bool atlasesFoldout;

        private Vector2 scrollPosition = Vector2.zero;

        private bool recordOnPlay = true;
        private VisualizationType visualizationType = VisualizationType.Scene;
        private SpriteAuditorEventForwarder spriteAuditorEventForwarder;
        private float spriteUsageSizeThreshold = 0.25f;
        
        
        private string[] spriteFilterNames;
        private int filter;

        [MenuItem("Tools/Sprite Auditor")]
        public static void OpenWindow()
        {
            SpriteAuditorWindow window = GetWindow<SpriteAuditorWindow>("Atlas Auditor");
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;


            GenerateFiltersOptions();
        }

        private void GenerateFiltersOptions()
        {
            spriteFilterNames = new[]
            {
                "Size Warnings",
                "Used In Only 1 scene",
                "Used on Dont Destroy On Load Scenes",
                "Unable to Detect Size",
                "Single Sprites",
                "Atlas Sprites"
            };
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
                    SaveAtlasResult();
                }
            }
        }

        private void LoadOrCreateAtlasResult()
        {
            string storedJson = EditorPrefs.GetString(ATLAS_AUDITOR_STORAGE_KEY, string.Empty);
            cachedSpriteDatabase = new SpriteDatabase(visualizationType);

            if (!string.IsNullOrEmpty(storedJson))
                JsonWrapper.FromJson(storedJson, ref cachedSpriteDatabase);
        }

        private void SaveAtlasResult()
        {
            string json = JsonWrapper.ToJson(SpriteDatabase);
            EditorPrefs.SetString(ATLAS_AUDITOR_STORAGE_KEY, json);
        }
        
        private void ClearCache()
        {
            EditorPrefs.DeleteKey(ATLAS_AUDITOR_STORAGE_KEY);
            cachedSpriteDatabase = new SpriteDatabase(visualizationType);
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

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Results", EditorStyles.toolbarDropDown);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUI.BeginChangeCheck();
            visualizationType =
                (VisualizationType) GUILayout.SelectionGrid((int) visualizationType, VISUALIZATION_NAMES, 2,
                    EditorStyles.radioButton);
            if (EditorGUI.EndChangeCheck())
            {
                SpriteDatabase.SetVisualizationType(visualizationType);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal("Box");

            filter = EditorGUILayout.MaskField("Filter Results", filter, spriteFilterNames);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            SpriteDatabase.DrawResults(visualizationType);

            if (GUILayout.Button("Refresh Results"))
                SpriteDatabase.RefreshResults(visualizationType);
            
            EditorGUILayout.EndVertical();
        }

        // private void DrawResultsByAtlas()
        // {
        //     if (SpriteDatabase.AtlasToUsedSprites.Count > 0)
        //     {
        //         EditorGUILayout.BeginVertical("Box");
        //
        //         if (DrawStringFoldout("Used Atlases", VisualizationType.Atlas.ToString()))
        //         {
        //             EditorGUI.indentLevel++;
        //             foreach (var atlasToUsedSprites in SpriteDatabase.AtlasToUsedSprites)
        //             {
        //                 EditorGUILayout.BeginVertical("Box");
        //
        //                 if (DrawObjectFoldout(atlasToUsedSprites.Key, $"{VisualizationType.Atlas.ToString()}_{atlasToUsedSprites.Key.name}"))
        //                 {
        //                     if (atlasToUsedSprites.Value.Count > 0)
        //                     {
        //                         EditorGUI.indentLevel++;
        //
        //                         EditorGUILayout.BeginVertical("Box");
        //
        //                         if (DrawStringFoldout("Used Sprites", $"{VisualizationType.Atlas.ToString()}_USED_SPRITES"))
        //                         {
        //                             EditorGUI.indentLevel++;
        //                             foreach (Sprite sprite in atlasToUsedSprites.Value)
        //                             {
        //                                 DrawSpriteField(sprite, atlasToUsedSprites.Key,null, SpriteDrawDetails.All,
        //                                     $"{VisualizationType.Atlas.ToString()}_{atlasToUsedSprites.Key.name}");
        //                             }
        //                             EditorGUI.indentLevel--;
        //                         }
        //                         EditorGUI.indentLevel--;
        //                         EditorGUILayout.EndVertical();
        //                     }
        //
        //                     if (SpriteDatabase.AtlasToNotUsedSprites[atlasToUsedSprites.Key].Count > 0)
        //                     {
        //                         EditorGUI.indentLevel++;
        //                         EditorGUILayout.BeginVertical("Box");
        //                         if (DrawStringFoldout("Not used sprites", $"{VisualizationType.Atlas.ToString()}_NOT_USED_SPRITES"))
        //                         {
        //                             EditorGUI.indentLevel++;
        //                             foreach (Sprite sprite in SpriteDatabase.AtlasToNotUsedSprites[atlasToUsedSprites.Key])
        //                             {
        //                                 DrawSpriteField(sprite, atlasToUsedSprites.Key,null, SpriteDrawDetails.None,
        //                                     $"{VisualizationType.Atlas.ToString()}_{atlasToUsedSprites.Key.name}");
        //                             }
        //                             EditorGUI.indentLevel--;
        //                         }
        //                         EditorGUILayout.EndVertical();
        //                         EditorGUI.indentLevel--;
        //                     }
        //                 }
        //                 EditorGUILayout.EndVertical();
        //             }
        //             EditorGUI.indentLevel--;
        //         }
        //         EditorGUILayout.EndVertical();
        //     }
        // }

       

        // private bool DrawObjectFoldout<T>(T targetObject, string foldoutKey, bool showFoldout = true) where T:Object
        // {
        //     if (!keyToFoldout.ContainsKey(foldoutKey))
        //         keyToFoldout.Add(foldoutKey, false);
        //     
        //     EditorGUILayout.BeginHorizontal();
        //
        //     if (showFoldout)
        //     {
        //         GUIStyle style = new GUIStyle(EditorStyles.foldout)
        //         {
        //             fixedWidth = 5
        //         };
        //             
        //         keyToFoldout[foldoutKey] = EditorGUILayout.Foldout(keyToFoldout[foldoutKey], "", true, style);
        //         GUILayout.Space(-34);
        //     }
        //             
        //     EditorGUILayout.ObjectField(targetObject, typeof(T), false);
        //     EditorGUILayout.EndHorizontal();
        //
        //     return keyToFoldout[foldoutKey];
        // }
        //
        //
        //
        // private void DrawSpriteField(Sprite sprite, SpriteAtlas atlas = null, SceneAsset sceneAsset = null,
        //     SpriteDrawDetails drawDetails = SpriteDrawDetails.All, string foldoutKey = "")
        // {
        //     EditorGUILayout.BeginVertical("Box");
        //
        //     if (DrawObjectFoldout(sprite, $"{foldoutKey}_{sprite.name}", !drawDetails.HasFlag(SpriteDrawDetails.None)))
        //     {
        //         EditorGUI.indentLevel++;
        //
        //         if (drawDetails.HasFlag(SpriteDrawDetails.UsageCount))
        //             DrawSpriteUsageCount(sprite);
        //
        //         if (drawDetails.HasFlag(SpriteDrawDetails.SizeDetails))
        //         {
        //             float scale = 1.0f;
        //             if (atlas != null)
        //                 scale = SpriteDatabase.AtlasToScale[atlas];
        //             DrawSpriteSizeDetails(sprite, scale);
        //         }    
        //
        //         if (drawDetails.HasFlag(SpriteDrawDetails.ReferencesPath))
        //             DrawSpriteReferencesPath(sceneAsset, sprite);
        //
        //         if (drawDetails.HasFlag(SpriteDrawDetails.SceneReferences))
        //             DrawSpriteSceneReferences(sprite);
        //         EditorGUI.indentLevel--;
        //     }
        //     EditorGUILayout.EndVertical();
        // }
        //
        //
        //
        // private void DrawSpriteSceneReferences(Sprite sprite)
        // {
        //     EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
        //     EditorGUI.indentLevel++;
        //
        //     foreach (var scene in SpriteDatabase.SpriteToScenes[sprite])
        //     {
        //         EditorGUILayout.ObjectField(scene, typeof(SceneAsset), false);
        //     }
        //
        //     EditorGUI.indentLevel--;
        // }
        //
        //
        //
        //
        //
        // private void DrawSpriteReferencesPath(SceneAsset sceneAsset, Sprite sprite)
        // {
        //     EditorGUILayout.LabelField("Usages", EditorStyles.boldLabel);
        //     EditorGUI.indentLevel++;
        //
        //     foreach (string usePath in SpriteDatabase.SpriteToUseTransformPath[sprite])
        //     {
        //         EditorGUILayout.BeginHorizontal();
        //         EditorGUILayout.LabelField(usePath);
        //         if (GUILayout.Button("Select", EditorStyles.miniButton))
        //         {
        //             TrySelectObjectAtPath(sceneAsset, usePath);
        //         }
        //
        //         EditorGUILayout.EndHorizontal();
        //     }
        //
        //     EditorGUI.indentLevel--;
        // }
        //
        //
        // private void DrawSpriteSizeDetails(Sprite sprite, float atlasScale = 1)
        // {
        //     EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
        //     EditorGUI.indentLevel++;
        //     Vector3 spriteMaxUseSize = SpriteDatabase.GetSpriteMaxUseSize(sprite);
        //     EditorGUILayout.LabelField(
        //         $"Max Use Size Width: {Mathf.RoundToInt(spriteMaxUseSize.x)} Height: {Mathf.RoundToInt(spriteMaxUseSize.y)}");
        //
        //     Vector2 spriteSize = sprite.rect.size;
        //     spriteSize = spriteSize * atlasScale;
        //     
        //     EditorGUILayout.LabelField(
        //         $"Sprite Size Width: {Mathf.RoundToInt(spriteSize.x)} Height: {Mathf.RoundToInt(spriteSize.y)}");
        //
        //     
        //     Vector3 sizeDifference = new Vector3(spriteMaxUseSize.x - spriteSize.x,
        //         spriteMaxUseSize.y - spriteSize.y, 0);
        //
        //     float differenceMagnitude = sizeDifference.magnitude / spriteSize.magnitude;
        //
        //     if (Mathf.Abs(differenceMagnitude) > spriteUsageSizeThreshold)
        //     {
        //         if (spriteMaxUseSize.sqrMagnitude > spriteSize.sqrMagnitude)
        //         {
        //             EditorGUILayout.HelpBox(
        //                 $"Sprite used with a size {differenceMagnitude:P} times bigger than imported sprite size," +
        //                 $" you may consider resizing it up",
        //                 MessageType.Warning);
        //         }
        //         else
        //         {
        //             EditorGUILayout.HelpBox(
        //                 $"Sprite used with a size {differenceMagnitude:P} times smaller than imported sprite size," +
        //                 $" you may consider resizing it down",
        //                 MessageType.Warning);
        //         }
        //     }
        //
        //     EditorGUI.indentLevel--;
        // }
        //
        //
        //
        //
        // private void TrySelectObjectAtPath(SceneAsset sceneAsset, string usePath)
        // {
        //     if (!Application.isPlaying)
        //     {
        //         if (sceneAsset != null)
        //             EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(sceneAsset), OpenSceneMode.Additive);
        //     }
        //     
        //     GameObject gameObject = GameObject.Find(usePath);
        //     if (gameObject == null)
        //         return;
        //     
        //     Selection.SetActiveObjectWithContext(gameObject, this);
        // }

        private void DrawSettings()
        {
            EditorGUI.BeginDisabledGroup(isRecording);
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Settings", EditorStyles.toolbarDropDown);
            EditorGUILayout.Space();

            recordOnPlay = EditorGUILayout.Toggle("Record on play", recordOnPlay);

            EditorGUI.BeginChangeCheck();
            spriteUsageSizeThreshold = 
                EditorGUILayout.Slider("Allowed Size Variation", spriteUsageSizeThreshold, 0, 2);
            if (EditorGUI.EndChangeCheck())
            {
                SpriteDatabase.SetAllowedSizeVariation(spriteUsageSizeThreshold);
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!EditorPrefs.HasKey(ATLAS_AUDITOR_STORAGE_KEY));
            if (GUILayout.Button("Clear Cache", EditorStyles.toolbarButton))
                ClearCache();
            
            if (GUILayout.Button("Pack Atlases", EditorStyles.toolbarButton))
                SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUI.EndDisabledGroup();
        }

        private void StopRecording()
        {
            if (!isRecording)
                return;
            
            if (spriteAuditorEventForwarder != null)
                Destroy(spriteAuditorEventForwarder.gameObject);
            
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
            
            spriteAuditorEventForwarder = spriteAuditorGameObject.AddComponent<SpriteAuditorEventForwarder>();
            spriteAuditorEventForwarder.SetListener(spriteFinder);
            DontDestroyOnLoad(spriteAuditorEventForwarder.gameObject);
        }
    }
}
