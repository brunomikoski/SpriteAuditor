using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrunoMikoski.SpriteAuditor
{
    public class SpriteFinder : IProjectUpdateLoopListener
    {
        private SpriteAuditorResult result;

        private List<Image> images = new List<Image>(1000);
        private List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>(1000);
        private List<SpriteMask> spriteMasks = new List<SpriteMask>(1000);
        private List<Button> buttons = new List<Button>(1000);
        
        public void SetResult(SpriteAuditorResult targetResult)
        {
            result = targetResult;
        }

        void IProjectUpdateLoopListener.OnProjectUpdate()
        {
            if (result == null)
                return;
            
            FindAllObjectsOfType(ref images);
            for (int i = 0; i < images.Count; i++)
            {
                Image image = images[i];
                result.ReportImage(image);
            }
            
            FindAllObjectsOfType(ref buttons);
            for (int i = 0; i < buttons.Count; i++)
            {
                Button button = buttons[i];
                result.ReportButton(button);
            }
            
            FindAllObjectsOfType(ref spriteRenderers);
            for (int i = 0; i < spriteRenderers.Count; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];

                result.ReportSpriteRenderer(spriteRenderer);
            }
            
            FindAllObjectsOfType(ref spriteMasks);
            for (int i = 0; i < spriteMasks.Count; i++)
            {
                SpriteMask spriteMask = spriteMasks[i];
                
                result.ReportSpriteMask(spriteMask);
            }
        }

        private static void FindAllObjectsOfType<T>(ref List<T> resultList, bool clearBefore = true)
        {
            if (clearBefore)
                resultList.Clear();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    GameObject[] allGameObjects = scene.GetRootGameObjects();
                    for (int j = 0; j < allGameObjects.Length; j++)
                    {
                        GameObject gameObject = allGameObjects[j];
                        resultList.AddRange(gameObject.GetComponentsInChildren<T>(true));
                    }
                }
            }
        }
    }
}
