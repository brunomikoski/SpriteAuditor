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
        

        private List<Camera> knowCameras = new List<Camera>();
        private Dictionary<int, Camera> layerToCamera = new Dictionary<int, Camera>();
        
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
                if (image.sprite == null)
                    continue;
                
                result.AddSprite(image.sprite, image.gameObject, image.GetPixelSize());
            }
            
            FindAllObjectsOfType(ref buttons);
            for (int i = 0; i < buttons.Count; i++)
            {
                Button button = buttons[i];
                
                if (button.targetGraphic is Image targetGraphicImage)
                {
                    Vector3 pixelSize = targetGraphicImage.GetPixelSize();

                    result.AddSprite(targetGraphicImage.sprite, button.gameObject, pixelSize);
                    result.AddSprite(button.spriteState.disabledSprite, button.gameObject, pixelSize);
                    result.AddSprite(button.spriteState.highlightedSprite, button.gameObject, pixelSize);
                    result.AddSprite(button.spriteState.pressedSprite, button.gameObject, pixelSize);
                }
            }
            
            FindAllObjectsOfType(ref spriteRenderers);
            for (int i = 0; i < spriteRenderers.Count; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];

                if (spriteRenderer.sprite == null)
                    continue;
                
                if (TryGetCameraForGameObject(spriteRenderer.gameObject, out Camera targetCamera))
                    result.AddSprite(spriteRenderer.sprite, spriteRenderer.gameObject, spriteRenderer.GetPixelSize(targetCamera));
            }
            
            FindAllObjectsOfType(ref spriteMasks);
            for (int i = 0; i < spriteMasks.Count; i++)
            {
                SpriteMask spriteMask = spriteMasks[i];
                if (spriteMask.sprite == null)
                    continue;
                
                if (TryGetCameraForGameObject(spriteMask.gameObject, out Camera targetCamera))
                    result.AddSprite(spriteMask.sprite, spriteMask.gameObject, spriteMask.GetPixelSize(targetCamera));
            }
        }

        private bool TryGetCameraForGameObject(GameObject gameObject, out Camera camera)
        {
            int gameObjectLayer = gameObject.layer;

            if (layerToCamera.TryGetValue(gameObjectLayer, out camera))
            {
                if (camera != null)
                {
                    return true;
                }
                layerToCamera.Remove(gameObjectLayer);
            }

            FindAllObjectsOfType(ref knowCameras);


            for (int i = 0; i < knowCameras.Count; i++)
            {
                Camera knowCamera = knowCameras[i];
                if ((knowCamera.cullingMask & (1 << gameObjectLayer)) != 0)
                {
                    camera = knowCamera;
                    layerToCamera.Add(gameObjectLayer, camera);
                    return true;
                }
            }
            
            camera = Camera.main;
            return false;
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
