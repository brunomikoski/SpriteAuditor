using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BrunoMikoski.SpriteAuditor
{
    public class SpriteFinder : IProjectUpdateLoopListener
    {
        private SpriteDatabase result;
   
        public void SetResult(SpriteDatabase targetResult)
        {
            result = targetResult;
        }

        void IProjectUpdateLoopListener.OnProjectUpdate()
        {
            Component[] components = Resources.FindObjectsOfTypeAll<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                
                if (component.hideFlags == HideFlags.NotEditable || component.hideFlags == HideFlags.HideAndDontSave)
                    continue;
                
                if (EditorUtility.IsPersistent(component))
                    continue;
            
                if (component is Image image)
                {
                    result.ReportImage(image);
                }
                else if (component is Button button)
                {
                    result.ReportButton(button);
                }
                else if (component is SpriteRenderer spriteRenderer)
                {
                    result.ReportSpriteRenderer(spriteRenderer);
                }
                else if (component is SpriteMask spriteMask)
                {
                    result.ReportSpriteMask(spriteMask);
                }
            }
        }

    }
}
