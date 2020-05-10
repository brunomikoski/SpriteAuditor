using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
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

        [SerializeField]
        private string nearestPrefabGUID;
        public string NearestPrefabGUID => nearestPrefabGUID;

        public int InstanceID => instanceID;


        public SpriteUseData(GameObject gameObject, int instanceID, string usagePath)
        {
            this.instanceID = instanceID;
            hierarchyPaths.Add(usagePath);

            if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
            {
                nearestPrefabGUID =
                    AssetDatabase.AssetPathToGUID(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject));
            }
        }

        public void ReportPath(string usagePath, Scene targetScene)
        {
            string targetScenePath = targetScene.path;
            string storagePath = $"{targetScenePath}{PATH_SEPARATOR}{usagePath}";

            if (paths.Count == 0)
                firstPath = storagePath;
            if (paths.Add(storagePath))
                SpriteAuditorUtility.SetResultViewDirty();
        }
    }
}
