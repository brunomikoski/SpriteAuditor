using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace BrunoMikoski.SpriteAuditor
{
    public static class AtlasUtility
    {
        private static Dictionary<SpriteAtlas, Sprite[]> atlasToAllSprites = new Dictionary<SpriteAtlas, Sprite[]>();
        private static Dictionary<SpriteAtlas, float> atlasToScale = new Dictionary<SpriteAtlas, float>();

        private static bool hasDataCached = false;
        
        private static void CacheKnowAtlases()
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

                if (atlas.isVariant)
                {
                    if (atlas.TryGetMasterAtlas(out SpriteAtlas masterAtlas))
                    {
                        atlasToScale.Add(atlas, atlas.GetVariantScale());
                        atlasToAllSprites.Add(atlas, masterAtlas.GetAllSprites().ToArray());
                    }
                }
                else
                {
                    atlasToAllSprites.Add(atlas, atlas.GetAllSprites().ToArray());
                    atlasToScale.Add(atlas, 1.0f);
                }
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

        public static float GetAtlasScale(SpriteAtlas spriteAtlas)
        {
            if (!hasDataCached)
                CacheKnowAtlases();
            
            if (atlasToScale.TryGetValue(spriteAtlas, out float scale))
                return scale;
            return 1;
        }
    }
}