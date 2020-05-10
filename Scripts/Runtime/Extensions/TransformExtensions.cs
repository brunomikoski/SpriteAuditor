using System.IO;

namespace UnityEngine
{
    public static partial class TransformExtensions
    {
        public static string GetPath(this Transform transform, Transform upToParent = null,
            string separator = "")
        {
            if (string.IsNullOrEmpty(separator))
                separator = Path.AltDirectorySeparatorChar.ToString();
            
            if (transform == upToParent)
                return "";

            if (transform.parent == upToParent)
                return transform.name;

            return GetPath(transform.parent, upToParent, separator) + separator + transform.name;
        }
    }
}