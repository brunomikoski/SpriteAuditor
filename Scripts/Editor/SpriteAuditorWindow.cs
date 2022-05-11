using System;
using BrunoMikoski.SpriteAuditor.Serialization;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEditor.U2D;
using UnityEngine;

namespace BrunoMikoski.SpriteAuditor
{
    public class SpriteAuditorWindow : EditorWindow
    {
        private const string ATLAS_AUDITOR_STORAGE_KEY = "ATLAS_AUDITOR_STORAGE_KEY";
        private static string[] VISUALIZATION_NAMES = {"Scene View", "Atlas View", "Sprite View"};
        
        private bool isRecording;
        
        private static SpriteFinder spriteFinder = new SpriteFinder();

        private SpriteDatabase cachedSpriteDatabase;
        public SpriteDatabase SpriteDatabase
        {
            get
            {
                if (cachedSpriteDatabase == null)
                    LoadOrCreateDatabase();
                return cachedSpriteDatabase;
            }
        }

        private bool recordOnUpdate = true;
        private VisualizationType visualizationType = VisualizationType.Scene;
        private SpriteAuditorEventForwarder spriteAuditorEventForwarder;
        private float spriteUsageSizeThreshold = 0.25f;

        private BaseResultView resultView;
        private int frameInterval = 1;
        private SearchField searchField;

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
        

        [MenuItem("Window/Analysis/Sprite Auditor")]
        public static void OpenWindow()
        {
            GetWindowInstance().Show();
        }

        public static SpriteAuditorWindow GetWindowInstance()
        {
            return GetWindow<SpriteAuditorWindow>("Atlas Auditor");
        }
        public static bool IsOpen()
        {
            return HasOpenInstances<SpriteAuditorWindow>();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            SpriteAuditorUtility.SetMemoryDataDirty();
            SpriteAuditorUtility.SetResultViewDirty();
            SpriteAuditorUtility.SetSizeCheckThreshold(spriteUsageSizeThreshold);
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
                    StartRecording();
                    break;
                }
                case PlayModeStateChange.ExitingPlayMode:
                {
                    if (isRecording)
                    {
                        StopRecording();
                        StoreDatabase();
                    }

                    break;
                }
                case PlayModeStateChange.EnteredEditMode:
                    SpriteAuditorUtility.SetResultViewDirty();
                    break;
            }
        }

        private void LoadOrCreateDatabase()
        {
            string storedJson = EditorPrefs.GetString(ATLAS_AUDITOR_STORAGE_KEY, string.Empty);
            cachedSpriteDatabase = new SpriteDatabase();

            if (!string.IsNullOrEmpty(storedJson))
                JsonWrapper.FromJson(storedJson, ref cachedSpriteDatabase);
            
            spriteFinder.SetResult(SpriteDatabase);
            SpriteAuditorUtility.ClearMemoryDataDirty();
        }

        private void StoreDatabase()
        {
            string json = JsonWrapper.ToJson(SpriteDatabase, false);
            EditorPrefs.SetString(ATLAS_AUDITOR_STORAGE_KEY, json);
            SpriteAuditorUtility.ClearSaveDataDirty();
        }
        
        private void ClearCache()
        {
            EditorPrefs.DeleteKey(ATLAS_AUDITOR_STORAGE_KEY);
            cachedSpriteDatabase = null;
            SpriteAuditorUtility.SetResultViewDirty();
        }
        
        private void OnGUI()
        {
            if (SpriteAuditorUtility.IsMemoryDataDirty)
                LoadOrCreateDatabase();

            if (SpriteAuditorUtility.IsIsSpriteDataDirty)
                UpdateSpriteData();

            if (SpriteAuditorUtility.IsSaveDataDirty)
                StoreDatabase();
                
            if (SpriteAuditorUtility.IsReferencesDirty)
            {
                ResultView.GenerateResults(SpriteDatabase);
                SpriteAuditorUtility.SetResultViewUpdated();
            }
            
            DrawSettings();
            DrawResults();
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
                (VisualizationType) GUILayout.SelectionGrid((int) visualizationType, VISUALIZATION_NAMES, 3,
                    EditorStyles.radioButton);
            if (EditorGUI.EndChangeCheck())
            {
                CreateResultViewByVisualizationType();
                SpriteAuditorUtility.ClearSelection();
                SpriteAuditorUtility.SetResultViewDirty();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal("Box");

            ResultView.DrawFilterOptions();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            DrawSearch();
            SpriteAuditorBatchAction.DrawBatchActions();
            ResultView.DrawResults(SpriteDatabase);
            
            EditorGUILayout.EndVertical();
        }

        private void UpdateSpriteData()
        {
            SpriteDatabase.UpdateSpriteData();
        }

        private void DrawSearch()
        {
            Rect searchRect =
                GUILayoutUtility.GetRect(1, 1, 18, 18, GUILayout.ExpandWidth(true));

            if (searchField == null)
                searchField = new SearchField();

            EditorGUI.BeginChangeCheck();
            string searchText = searchField.OnGUI(searchRect, SpriteAuditorUtility.SearchText);
            if (EditorGUI.EndChangeCheck())
            {
                SpriteAuditorUtility.SearchText = searchText;
            }
            EditorGUILayout.Separator();
        }
        
        private void DrawSettings()
        {
            EditorGUI.BeginDisabledGroup(isRecording);
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Settings", EditorStyles.toolbarDropDown);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            recordOnUpdate = EditorGUILayout.Toggle("Record Automatically", recordOnUpdate);
            if (EditorGUI.EndChangeCheck())
                spriteFinder.SetCaptureOnUpdate(recordOnUpdate);
            
            if (recordOnUpdate)
            {
                EditorGUI.BeginChangeCheck();
                frameInterval = EditorGUILayout.IntField("Frame Interval", frameInterval);
                if (EditorGUI.EndChangeCheck())
                {
                    spriteFinder.SetFrameInterval(frameInterval);
                }
            }
            else
            {
                bool guiWasEnabled = GUI.enabled;
                GUI.enabled = true;
                if (GUILayout.Button("Capture Frame"))
                    spriteFinder.CaptureFrame();

                GUI.enabled = guiWasEnabled;
            }
            
            EditorGUI.BeginChangeCheck();
            spriteUsageSizeThreshold = 
                EditorGUILayout.Slider("Allowed Size Variation", spriteUsageSizeThreshold, 0, 2);
            if (EditorGUI.EndChangeCheck())
            {
                SpriteAuditorUtility.SetSizeCheckThreshold(spriteUsageSizeThreshold);
                SpriteDatabase.SizeCheckThresholdChanged();
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
            if (isRecording)
                return;
            isRecording = true;

            LoadOrCreateDatabase();
            SpriteDatabase.PrepareForRun();
            
            GameObject spriteAuditorGameObject = new GameObject("Sprite Auditor Forwarder");
            
            spriteAuditorEventForwarder = spriteAuditorGameObject.AddComponent<SpriteAuditorEventForwarder>();
            spriteFinder.SetFrameInterval(frameInterval);
            spriteFinder.SetCaptureOnUpdate(recordOnUpdate);
            spriteAuditorEventForwarder.SetListener(spriteFinder);
            DontDestroyOnLoad(spriteAuditorEventForwarder.gameObject);
        }
    }
}
