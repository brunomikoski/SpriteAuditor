using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;

namespace UnityEngine.U2D
{
    public static partial class EditorSpriteAtlasExtensions
    {
        public static float GetVariantScale(this SpriteAtlas spriteAtlas)
        {
            SerializedObject serObj = new SerializedObject(spriteAtlas);
            SerializedProperty iter = serObj.GetIterator();
            while (iter.Next(true)) 
            {
                if (string.Equals("m_EditorData.variantMultiplier", iter.propertyPath, StringComparison.Ordinal))
                    return iter.floatValue;
            }
            return 1.0f;
        }
        public static bool TryGetMasterAtlas(this SpriteAtlas spriteAtlas, out SpriteAtlas masterAtlas)
        {
            SerializedObject serObj = new SerializedObject(spriteAtlas);
            SerializedProperty iter = serObj.GetIterator();
            while (iter.Next(true)) 
            {
                if (string.Equals("m_MasterAtlas", iter.propertyPath, StringComparison.Ordinal))
                {
                    masterAtlas = iter.objectReferenceValue as SpriteAtlas;
                    return true;
                }
            }
            masterAtlas = null;
            return false;
        }
        public static bool IsIncludedInBuild(this SpriteAtlas spriteAtlas)
        {
            SerializedObject serObj = new SerializedObject(spriteAtlas);
            SerializedProperty iter = serObj.GetIterator();
            while (iter.Next(true)) 
            {
                if (string.Equals("m_EditorData.bindAsDefault", iter.propertyPath, StringComparison.Ordinal))
                    return iter.boolValue;
            }
            return false;
        }
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
                else if (packable is Sprite || packable is Texture2D)
                {
                    string path = AssetDatabase.GetAssetPath(packable);
                    resultSprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).Where(o => o is Sprite)
                        .Cast<Sprite>().ToArray());
                }
            }

            return resultSprites;
        }

        private static List<Sprite> GetAllSpritesFromFolder(string targetFolder)
        {
            List<Sprite> result = new List<Sprite>();
            string[] spritesGUIDs = AssetDatabase.FindAssets("t:Sprite", new[] {targetFolder});
            Uri rootFolder = new Uri(Path.GetFullPath(targetFolder));
            for (int i = 0; i < spritesGUIDs.Length; i++)
            {
                string spritesGUID = spritesGUIDs[i];
                string spritePath = AssetDatabase.GUIDToAssetPath(spritesGUID);
                Uri spriteUri = new Uri(Path.GetFullPath(spritePath));

                if (rootFolder != spriteUri && rootFolder.IsBaseOf(spriteUri))
                {
                    Sprite[] allSprites = AssetDatabase.LoadAllAssetsAtPath(spritePath).Where(o => o is Sprite)
                        .Cast<Sprite>().ToArray();
                    result.AddRange(allSprites);
                }
            }

            return result;
        }
    }
}
