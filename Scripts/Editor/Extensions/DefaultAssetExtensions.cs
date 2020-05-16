using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace UnityEngine
{
    public static partial class DefaultAssetExtensions
    {
        public static List<T> GetChildrenObjectsOfType<T>(this DefaultAsset targetDefaultAsset) where T : Object
        {
            string targetFolder = AssetDatabase.GetAssetPath(targetDefaultAsset);
            List<T> result = new List<T>();
            
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] {targetFolder});
            Uri rootFolder = new Uri(Path.GetFullPath(targetFolder));
            for (int i = 0; i < guids.Length; i++)
            {
                string objectPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Uri spriteUri = new Uri(Path.GetFullPath(objectPath));

                if (rootFolder != spriteUri && rootFolder.IsBaseOf(spriteUri))
                {
                    IEnumerable<T> foundObjects = AssetDatabase.LoadAllAssetsAtPath(objectPath).Where(o => o is T)
                        .Cast<T>();
                    result.AddRange(foundObjects);
                }
            }

            return result;
        }
    }
}