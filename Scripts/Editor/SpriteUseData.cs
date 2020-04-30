using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrunoMikoski.SpriteAuditor
{
    [Serializable]
    public class SpriteUseData
    {
        [SerializeField]
        private HashSet<string> paths = new HashSet<string>();

        [SerializeField]
        private string firstPath;
        public string FirstPath => firstPath;

        [SerializeField]
        private int instanceID;
        public int InstanceID => instanceID;

        public SpriteUseData(int instanceID)
        {
            this.instanceID = instanceID;
        }

        public void ReportPath(string usagePath, Scene targetScene)
        {
            string storagePath = $"{targetScene.path}:{usagePath}";

            if (paths.Count == 0)
                firstPath = storagePath;
            paths.Add(storagePath);
        }
    }
}
