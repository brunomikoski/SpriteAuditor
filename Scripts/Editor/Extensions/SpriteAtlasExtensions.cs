using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;

namespace UnityEngine.U2D
{
    public static partial class SpriteAtlasExtensions
    {
        public static List<Sprite> GetAllSprites(this SpriteAtlas spriteAtlas)
        {
            List<Sprite> resultSprites = new List<Sprite>();
            Object[] objects = spriteAtlas.GetPackables();
            for (int i = 0; i < objects.Length; i++)
            {
                Object packable = objects[i];

                if (packable is DefaultAsset defaultAsset)
                {
                    List<Sprite> sprites = GetAllSpritesFromFolder(AssetDatabase.GetAssetPath(defaultAsset));
                    resultSprites.AddRange(sprites);
                }
                else if (packable is Sprite sprite)
                {
                    resultSprites.Add(sprite);
                }
            }

            return resultSprites;
        }

        private static List<Sprite> GetAllSpritesFromFolder(string targetFolder)
        {
            List<Sprite> result = new List<Sprite>();
            string[] spritesGUIDs = AssetDatabase.FindAssets("t:Sprite");
            Uri rootFolder = new Uri(Path.GetFullPath(targetFolder));
            for (int i = 0; i < spritesGUIDs.Length; i++)
            {
                string spritesGUID = spritesGUIDs[i];
                string spritePath = AssetDatabase.GUIDToAssetPath(spritesGUID);
                Uri spriteUri = new Uri(Path.GetFullPath(spritePath));


                if (rootFolder != spriteUri && rootFolder.IsBaseOf(spriteUri))
                {
                    result.Add(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath));
                }
            }

            return result;
        }
    }
}
