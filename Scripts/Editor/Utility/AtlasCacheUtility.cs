using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace BrunoMikoski.SpriteAuditor
{
    public static class AtlasCacheUtility
    {
        private static Dictionary<SpriteAtlas, Sprite[]> atlasToAllSprites = new Dictionary<SpriteAtlas, Sprite[]>();
        private static Dictionary<SpriteAtlas, float> atlasToScale = new Dictionary<SpriteAtlas, float>();

        private static bool hasDataCached;
        
        public static void CacheKnowAtlases()
        {
            atlasToAllSprites.Clear();
            atlasToScale.Clear();

            string[] atlasGUIDs = AssetDatabase.FindAssets("t:SpriteAtlas");
            
            for (int i = 0; i < atlasGUIDs.Length; i++)
            {
                SpriteAtlas atlas =
                    AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(atlasGUIDs[i]));

                if (!atlas.IsIncludedInBuild())
                    continue;

                Sprite[] sprites = atlas.GetAllSprites().Distinct().ToArray();

                float scale = 1.0f;
                if (atlas.isVariant)
                {
                    if (atlas.TryGetMasterAtlas(out SpriteAtlas masterAtlas))
                        sprites = masterAtlas.GetAllSprites().Distinct().ToArray();

                    scale = atlas.GetVariantScale();
                }
                
                atlasToAllSprites.Add(atlas, sprites);
                atlasToScale.Add(atlas, scale);
            }

            hasDataCached = true;
        }

        public static bool TryGetAtlasForSprite(Sprite targetSprite, out SpriteAtlas spriteAtlas)
        {
            if (!hasDataCached)
                CacheKnowAtlases();
            
            foreach (var atlasToSprites in atlasToAllSprites)
            {
                for (int i = 0; i < atlasToSprites.Value.Length; i++)
                {
                    Sprite sprite = atlasToSprites.Value[i];
                    if (sprite == targetSprite)
                    {
                        spriteAtlas = atlasToSprites.Key;
                        return true;
                    }
                }
            }
            spriteAtlas = null;
            return false;
        }

        public static bool TryGetAtlasScale(SpriteAtlas spriteAtlas, out float atlasScale)
        {
            if (!hasDataCached)
                CacheKnowAtlases();

            atlasScale = 1;
            return atlasToScale.TryGetValue(spriteAtlas, out atlasScale); 
        }

        public static List<SpriteAtlas> GetAllKnowAtlases()
        {
            List<SpriteAtlas> atlases = new List<SpriteAtlas>();
            string[] atlasGUIDs = AssetDatabase.FindAssets("t:SpriteAtlas");
            
            for (int i = 0; i < atlasGUIDs.Length; i++)
            {
                SpriteAtlas atlas =
                    AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(atlasGUIDs[i]));

                atlases.Add(atlas);
            }

            return atlases;
        }

        public static void ClearAtlasCache()
        {
            hasDataCached = false;
        }

        public static Sprite[] GetAllSpritesFromAtlas(SpriteAtlas atlas)
        {
            if (!hasDataCached)
                CacheKnowAtlases();
            
            if (atlasToAllSprites.TryGetValue(atlas, out Sprite[] sprites))
                return sprites;

            return new Sprite[0];
        }
    }
}
