using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.SpriteAuditor
{
    public static class SpriteAuditorGUIUtility
    {
        private static Dictionary<string, bool> keyToFoldout = new Dictionary<string, bool>();

        public static void DrawFixSpriteSize(SpriteData spriteData)
        {
            if (!SpriteAuditorUtility.CanFixSpriteData(spriteData))
                return;
            
            GUIStyle button = new GUIStyle(EditorStyles.miniButton)
            {
                fixedWidth = 200
            };
            
            if (GUILayout.Button("Fix texture Size", button))
            {
                SpriteAuditorUtility.SetBestSizeForTexture(spriteData);
            }
        }

        public static bool DrawStringFoldout(string label, string foldoutKey)
        {
            if(!keyToFoldout.ContainsKey(foldoutKey))
                keyToFoldout.Add(foldoutKey, false);

            keyToFoldout[foldoutKey] = EditorGUILayout.Foldout(keyToFoldout[foldoutKey], label, true,
                EditorStyles.foldout);
            return keyToFoldout[foldoutKey];
        }
        
        public static bool DrawObjectFoldout<T>(T targetObject, string foldoutKey, bool showFoldout = true) where T : Object
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
    }
}
