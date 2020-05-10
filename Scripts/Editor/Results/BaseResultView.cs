using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;

namespace BrunoMikoski.SpriteAuditor
{
    public abstract class BaseResultView
    {
        private SearchField searchField;
        private Vector2 scrollPosition = Vector2.zero;

        public abstract void DrawFilterOptions();
        
        public abstract void GenerateResults(SpriteDatabase spriteDatabase);

        protected abstract void DrawResultsInternal(SpriteDatabase spriteDatabase);

        public void DrawResults(SpriteDatabase spriteDatabase)
        {
            DrawSearch();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false);

            DrawResultsInternal(spriteDatabase);
            EditorGUILayout.EndScrollView();
        }

        protected bool MatchSearch(string name)
        {
            if (string.IsNullOrEmpty(SpriteAuditorUtility.SearchText))
                return true;

            for (int i = 0; i < SpriteAuditorUtility.SearchSplitByComma.Length; i++)
            {
                string searchTerm = SpriteAuditorUtility.SearchSplitByComma[i];
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    if (name.IndexOf(searchTerm, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
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

        protected virtual void DrawSpriteDataField(SpriteData spriteData)
        {
            EditorGUILayout.BeginVertical("Box");
            if (SpriteAuditorGUIUtility.DrawObjectFoldout(spriteData.Sprite, spriteData.Sprite.name))
            {
                EditorGUI.indentLevel++;

                DrawSpriteUsageCount(spriteData);

                DrawSpriteSizeDetails(spriteData);

                DrawSpriteReferencesPath(spriteData);

                DrawSpriteSceneReferences(spriteData);

                DrawWarnings(spriteData);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
        
        protected bool ValidSpriteData(SpriteData data)
        {
            if (data.SpriteUsageFlags.HasFlag(SpriteUsageFlags.DefaultUnityAsset))
                return false;
            
            if (data.Sprite == null)
                return false;

            return true;
        }

        private void DrawWarnings(SpriteData spriteData)
        {
            if(!spriteData.HasWarnings())
                return;
            
            EditorGUILayout.LabelField("Warnings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(spriteData.SpriteUsageFlags.ToString());

            if (spriteData.SpriteUsageFlags.HasFlag(SpriteUsageFlags.CantDiscoveryAllUsageSize))
            {
                EditorGUILayout.LabelField(new GUIContent(
                    $"Cannot detect all sprite usages sizes!", SpriteAuditorGUIUtility.WarningIcon));
            }

            if (spriteData.MaximumUsageSize.HasValue)
            {
                float differenceMagnitude = spriteData.MaximumUsageSize.Value.magnitude /
                                            spriteData.SpriteSize.magnitude;
            
                if (spriteData.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsedBiggerThanSpriteRect))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent(
                        $"Sprite is used {Mathf.Abs(1.0f-differenceMagnitude):P} bigger than original Sprite, you may scale it up",
                        SpriteAuditorGUIUtility.WarningIcon));

                    SpriteAuditorGUIUtility.DrawFixSpriteSize(spriteData);
                    EditorGUILayout.EndHorizontal();
                }

                if (spriteData.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsedSmallerThanSpriteRect))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent(
                        $"Sprite is used {Mathf.Abs(1.0f - differenceMagnitude):P} smaller than original Sprite, you may scale it down",
                        SpriteAuditorGUIUtility.WarningIcon));


                    SpriteAuditorGUIUtility.DrawFixSpriteSize(spriteData);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
        }

        protected void DrawSpriteSceneReferences(SpriteData spriteData)
        {
            EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            foreach (SceneAsset sceneAsset in spriteData.SceneAssets)
            {
                EditorGUILayout.ObjectField(sceneAsset, typeof(SceneAsset), false);
            }

            EditorGUI.indentLevel--;
        }

        protected void DrawSpriteReferencesPath(SpriteData spriteData)
        {
            EditorGUILayout.LabelField("Usages", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            for (int i = 0; i < spriteData.Usages.Count; i++)
            {
                SpriteUseData spriteUseData = spriteData.Usages[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(spriteUseData.DisplayFirstPath);

                if (GUILayout.Button("Select", EditorStyles.miniButton))
                    TrySelect(spriteUseData);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        protected void DrawSpriteUsageCount(SpriteData spriteData)
        {
            EditorGUILayout.LabelField($"Total Usages Found", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"{spriteData.Usages.Count}");
            EditorGUI.indentLevel--;
        }

        protected void DrawSpriteSizeDetails(SpriteData spriteData)
        {
            EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Instances", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (spriteData.MaximumUsageSize.HasValue)
            {
                Vector3 maxUseSize = spriteData.MaximumUsageSize.Value;
                if (maxUseSize != Vector3.zero)
                {
                    EditorGUILayout.LabelField(
                        $"Max: {Mathf.RoundToInt(maxUseSize.x)} Height: {Mathf.RoundToInt(maxUseSize.y)}");
                }    
            }

            EditorGUI.indentLevel--;

            Vector2 spriteSize = spriteData.SpriteSize;
            EditorGUILayout.LabelField(
                $"Sprite Rect Size Width: {Mathf.RoundToInt(spriteSize.x)} Height: {Mathf.RoundToInt(spriteSize.y)}");

            EditorGUI.indentLevel--;
        }

        private void TrySelect(SpriteUseData spriteUseData)
        {
            if (!string.IsNullOrEmpty(spriteUseData.NearestPrefabGUID))
            {
                if (!Application.isPlaying)
                {
                     GameObject prefabInstance = PrefabUtility.LoadPrefabContents(AssetDatabase.GUIDToAssetPath(spriteUseData.NearestPrefabGUID));
                }
            }
            else
            {
                string[] paths = spriteUseData.FirstPath.Split(new[] {SpriteUseData.PATH_SEPARATOR},
                    StringSplitOptions.RemoveEmptyEntries);
                
                if (paths.Length != 2)
                {
                    return;
                }

                string scenePath = paths[0];
                if (!string.IsNullOrEmpty(scenePath))
                {
                    if (!scenePath.Contains("DontDestroyOnLoad"))
                    {
                        if (!Application.isPlaying)
                            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    }
                }

                GameObject gameObject = GameObject.Find(paths[1]);
                if (gameObject == null)
                    return;

                Selection.SetActiveObjectWithContext(gameObject, null);
            }
        }

        public void SetAllowedSizeVariation(float spriteUsageSizeThreshold)
        {
            //TODO
        }

    }
}
