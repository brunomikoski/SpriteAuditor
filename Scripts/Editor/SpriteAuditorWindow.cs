using System;
using BrunoMikoski.SpriteAuditor.Serialization;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.U2D;
using UnityEngine;

namespace BrunoMikoski.SpriteAuditor
{
    public class SpriteAuditorWindow : EditorWindow
    {
        private const string ATLAS_AUDITOR_STORAGE_KEY = "ATLAS_AUDITOR_STORAGE_KEY";
        private static string[] VISUALIZATION_NAMES = {"Scene View", "Atlas View", "Sprite View"};
        
        private bool isRecording;
        
        private SpriteFinder spriteFinder = new SpriteFinder();

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


        private Vector2 scrollPosition = Vector2.zero;

        private bool recordOnPlay = true;
        private VisualizationType visualizationType = VisualizationType.Scene;
        private SpriteAuditorEventForwarder spriteAuditorEventForwarder;
        private float spriteUsageSizeThreshold = 0.25f;

        private BaseResultView resultView;
        private BaseResultView ResultView
        {
            get
            {
                if (resultView == null)
                    CreateResultViewByVisualizationType();
                return resultView;
            }
        }
        
        private void CreateResultViewByVisualizationType()
        {
            switch (visualizationType)
            {
                case VisualizationType.Scene:
                    resultView = new SceneResultView();
                    break;
                case VisualizationType.Atlas:
                    resultView = new AtlasResultView();
                    break;
                case VisualizationType.Sprite:
                    resultView = new SpriteResultView();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        

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

        [DidReloadScripts]
        public static void ScriptsReloaded()
        {
            SpriteAuditorUtility.SetMemoryDataDirty();
            SpriteAuditorUtility.SetResultViewDirty();
        }
        private void OnPlayModeChanged(PlayModeStateChange playMode)
        {
            switch (playMode)
            {
                case PlayModeStateChange.EnteredPlayMode:
                {
                    if (recordOnPlay)
                        StartRecording();
                    break;
                }
                case PlayModeStateChange.ExitingPlayMode:
                {
                    if (isRecording)
                    {
                        StopRecording();
                        SaveAtlasResult();
                    }

                    break;
                }
                case PlayModeStateChange.EnteredEditMode:
                    SpriteAuditorUtility.SetResultViewDirty();
                    break;
            }
        }

        private void LoadOrCreateAtlasResult()
        {
            string storedJson = EditorPrefs.GetString(ATLAS_AUDITOR_STORAGE_KEY, string.Empty);
            cachedSpriteDatabase = new SpriteDatabase();

            if (!string.IsNullOrEmpty(storedJson))
                JsonWrapper.FromJson(storedJson, ref cachedSpriteDatabase);
            
            spriteFinder.SetResult(SpriteDatabase);
            SpriteAuditorUtility.MemoryDataLoaded();
        }

        private void SaveAtlasResult()
        {
            string json = JsonWrapper.ToJson(SpriteDatabase, false);
            EditorPrefs.SetString(ATLAS_AUDITOR_STORAGE_KEY, json);
        }
        
        private void ClearCache()
        {
            EditorPrefs.DeleteKey(ATLAS_AUDITOR_STORAGE_KEY);
            cachedSpriteDatabase = null;
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
            if (SpriteAuditorUtility.IsMemoryDataDirty)
                LoadOrCreateAtlasResult();
            
            if (SpriteDatabase == null)
                return;

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Results", EditorStyles.toolbarDropDown);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUI.BeginChangeCheck();
            visualizationType =
                (VisualizationType) GUILayout.SelectionGrid((int) visualizationType, VISUALIZATION_NAMES, 3,
                    EditorStyles.radioButton);
            if (EditorGUI.EndChangeCheck())
            {
                CreateResultViewByVisualizationType();
                SpriteAuditorUtility.SetResultViewDirty();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal("Box");

            ResultView.DrawFilterOptions();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            if (SpriteAuditorUtility.IsReferencesDirty)
            {
                AtlasCacheUtility.ClearAtlasCache();
                ResultView.GenerateResults(SpriteDatabase);
                SpriteAuditorUtility.SetResultViewUpdated();
            }
            
            ResultView.DrawResults(SpriteDatabase);
            
            EditorGUILayout.EndVertical();
        }
        
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
                //TODO 
                //SpriteDatabase.SetAllowedSizeVariation(spriteUsageSizeThreshold);
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!EditorPrefs.HasKey(ATLAS_AUDITOR_STORAGE_KEY));
            if (GUILayout.Button("Clear Cache", EditorStyles.toolbarButton))
                ClearCache();
            
            if (GUILayout.Button("Pack Atlases", EditorStyles.toolbarButton))
                SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);

            if (GUILayout.Button("Refresh Atlases", EditorStyles.toolbarButton))
                AtlasCacheUtility.CacheKnowAtlases();
            
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
        }

        private void StartRecording()
        {
            if(isRecording)
                return;
            isRecording = true;

            AtlasCacheUtility.ClearAtlasCache();
            LoadOrCreateAtlasResult();
            SpriteDatabase.PrepareForRun();
            
            GameObject spriteAuditorGameObject = new GameObject("Sprite Auditor Forwarder");
            
            spriteAuditorEventForwarder = spriteAuditorGameObject.AddComponent<SpriteAuditorEventForwarder>();
            spriteAuditorEventForwarder.SetListener(spriteFinder);
            DontDestroyOnLoad(spriteAuditorEventForwarder.gameObject);
        }
    }
}
