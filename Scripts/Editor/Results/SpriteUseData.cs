using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrunoMikoski.SpriteAuditor
{
    [Serializable]
    public class SpriteUseData
    {
        public const string PATH_SEPARATOR = ":*:";

        [SerializeField]
        private HashSet<string> paths = new HashSet<string>();
        public HashSet<string> Paths => paths;
        
        [SerializeField]
        private HashSet<string> hierarchyPaths = new HashSet<string>();
        public HashSet<string> HierarchyPaths => hierarchyPaths;

        [SerializeField]
        private string firstPath;
        public string FirstPath => firstPath;


        private string cachedDisplayFirstPath;
        public string DisplayFirstPath
        {
            get
            {
                if (string.IsNullOrEmpty(cachedDisplayFirstPath))
                {
                    string[] splited = firstPath.Split(new[] {PATH_SEPARATOR}, StringSplitOptions.RemoveEmptyEntries);
                    if (splited.Length != 2)
                        return firstPath;
                    string sceneName = Path.GetFileName(splited[0]);

                    cachedDisplayFirstPath = $"{sceneName}->{splited[1]}";
                }
                return cachedDisplayFirstPath;
            }
        }

        [SerializeField]
        private int instanceID;
        public int InstanceID => instanceID;


        public SpriteUseData(int instanceID, string usagePath)
        {
            this.instanceID = instanceID;
            hierarchyPaths.Add(usagePath);
        }

        public void ReportPath(string usagePath, Scene targetScene)
        {
            string storagePath = $"{targetScene.path}{PATH_SEPARATOR}{usagePath}";

            if (paths.Count == 0)
                firstPath = storagePath;
            paths.Add(storagePath);
        }
    }
}
