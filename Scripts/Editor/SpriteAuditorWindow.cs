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


        private bool refreshedResultsOnce = false;
        
        private ResultsFilter currentFilter = (ResultsFilter)  ~0;

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

            EditorGUI.BeginChangeCheck();
            currentFilter = (ResultsFilter) EditorGUILayout.EnumFlagsField("Filter", currentFilter);

            if (EditorGUI.EndChangeCheck())
                SpriteDatabase.RefreshResults(currentFilter);
            
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            if (!refreshedResultsOnce)
            {
                SpriteDatabase.RefreshResults(currentFilter);
                refreshedResultsOnce = true;
            }
            
            SpriteDatabase.DrawResults();

            if (GUILayout.Button("Refresh Results"))
                SpriteDatabase.RefreshResults(currentFilter);
            
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
