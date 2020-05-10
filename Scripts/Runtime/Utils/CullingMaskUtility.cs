using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.SpriteAuditor.Utils
{
    public static class CullingMaskUtility
    {
        private static Camera[] knowCameras = new Camera[0];
        private static readonly Dictionary<int, Camera> layerToCamera = new Dictionary<int, Camera>();
        
        public static bool TryGetCameraForGameObject(GameObject gameObject, out Camera camera, bool searchForNewCameras = true)
        {
            int gameObjectLayer = gameObject.layer;

            if (layerToCamera.TryGetValue(gameObjectLayer, out camera))
            {
                if (camera != null)
                    return true;
                
                layerToCamera.Remove(gameObjectLayer);
            }

            for (int i = 0; i < knowCameras.Length; i++)
            {
                Camera knowCamera = knowCameras[i];
                if (knowCamera == null)
                    continue;
                
                if ((knowCamera.cullingMask & (1 << gameObjectLayer)) != 0)
                {
                    camera = knowCamera;
                    layerToCamera.Add(gameObjectLayer, camera);
                    return true;
                }
            }

            if (searchForNewCameras)
            {
                knowCameras = Object.FindObjectsOfType<Camera>();
                return TryGetCameraForGameObject(gameObject, out camera, false);
            }
            
            camera = null;
            return false;
        }
        
    }
}
