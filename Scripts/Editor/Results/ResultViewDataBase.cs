using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.SpriteAuditor
{
    public abstract class ResultViewDataBase
    {
        private float allowedSizeVariation = 0.25f;
        
        public abstract void GenerateResults(SpriteDatabase spriteDatabase, ResultsFilter currentFilter);

        public abstract void DrawResults(SpriteDatabase spriteDatabase);

        protected virtual void DrawSpriteDataField(SpriteData spriteData)
        {
            EditorGUILayout.BeginVertical("Box");
            if (EditorGUIHelpers.DrawObjectFoldout(spriteData.Sprite, spriteData.Sprite.name,
                true))
            {
                EditorGUI.indentLevel++;

                DrawSpriteUsageCount(spriteData);

                DrawSpriteSizeDetails(spriteData);

                DrawSpriteReferencesPath(spriteData);

                DrawSpriteSceneReferences(spriteData);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
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

            if (spriteData.MinimumUsageSize.HasValue)
            {
                Vector3 minUseSize = spriteData.MinimumUsageSize.Value;
                if (minUseSize != Vector3.zero)
                {
                    EditorGUILayout.LabelField(
                        $"Min: {Mathf.RoundToInt(minUseSize.x)} Height: {Mathf.RoundToInt(minUseSize.y)}");
                }
            }

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
            Object targetInstance = EditorUtility.InstanceIDToObject(spriteUseData.InstanceID);
            if (targetInstance != null)
            {
                Selection.SetActiveObjectWithContext(targetInstance, null);
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
                if (string.IsNullOrEmpty(scenePath))
                    return;

                if (!Application.isPlaying)
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }

                GameObject gameObject = GameObject.Find(paths[1]);
                if (gameObject == null)
                    return;

                Selection.SetActiveObjectWithContext(gameObject, null);
            }
        }

        public void SetAllowedSizeVariation(float spriteUsageSizeThreshold)
        {
            allowedSizeVariation = spriteUsageSizeThreshold;
        }
    }
}