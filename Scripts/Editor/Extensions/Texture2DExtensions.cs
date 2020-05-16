using System.Linq;
using UnityEditor;

namespace UnityEngine
{
    public static partial class Texture2DExtensions
    {
        public static bool TryLoadSprites(this Texture2D texture, out Sprite[] sprites )
        {
            string texturePath = AssetDatabase.GetAssetPath(texture);
            sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                .Where(o => o is Sprite).Cast<Sprite>().ToArray();

            return sprites.Length > 0;
        }
    }
}