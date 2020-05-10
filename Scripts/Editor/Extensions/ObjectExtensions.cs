namespace UnityEngine
{
    public static partial class ObjectExtensions
    {
        public static string GetGUID(this Object targetObject)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(targetObject);
            string spriteGUID = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);

            return spriteGUID;
        }
        
    }
}