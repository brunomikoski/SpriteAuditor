using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.SpriteAuditor
{
    public static class SpriteAuditorGUIUtility
    {
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
    }
}