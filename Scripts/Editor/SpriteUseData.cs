using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrunoMikoski.SpriteAuditor
{
    [Serializable]
    internal class SpriteUseData
    {
        [SerializeField]
        private HashSet<string> paths = new HashSet<string>();
        [SerializeField]
        private HashSet<string> scenesPath = new HashSet<string>();
        [SerializeField]
        private int instanceID;
        public int InstanceID => instanceID;

        private bool usedOnDontDestroyOnLoadScene;

        public SpriteUseData(int instanceID)
        {
            this.instanceID = instanceID;
        }

        public void ReportPath(string usagePath, Scene targetScene)
        {
            paths.Add($"{targetScene.path}:{usagePath}");
        }

        public void ReportScene(Scene scene)
        {
            //-1 is the Dont Destroy On load Scene
            if (scene.buildIndex == -1)
                usedOnDontDestroyOnLoadScene = true;
            else
                scenesPath.Add(scene.path);
        }

        
    }
}
