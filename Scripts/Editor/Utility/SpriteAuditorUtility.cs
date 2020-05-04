using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.SpriteAuditor
{
    public static class SpriteAuditorUtility
    {
        private static int[] AVAILABLE_SPRITE_SIZES = {32, 64, 128, 256, 512, 1024, 2048, 4096, 8192};

        public static void SetBestSizeForTexture(SpriteData spriteData)
        {
            TryFindSmallerSizeTexture(spriteData, out int smallerSize);

            spriteData.TextureImporter.maxTextureSize = smallerSize;
            spriteData.TextureImporter.SaveAndReimport();
            spriteData.CheckForSizeFlags();
        }

        private static bool TryFindSmallerSizeTexture(SpriteData spriteData, out int smallerSize)
        {
            if (!spriteData.MaximumUsageSize.HasValue)
            {
                smallerSize = -1;
                return false;
            }
            
            int maxSize = Mathf.RoundToInt(Mathf.Max(spriteData.MaximumUsageSize.Value.x,
                spriteData.MaximumUsageSize.Value.y));

            for (int i = 0; i < AVAILABLE_SPRITE_SIZES.Length; i++)
            {
                int size = AVAILABLE_SPRITE_SIZES[i];
                if (size < maxSize)
                    continue;

                smallerSize = size;
                return true;
            }

            smallerSize = -1;
            return false;
        }

        public static bool CanFixSpriteData(SpriteData spriteData)
        {
            if (spriteData.TextureImporter == null)
                return false;
            
            if (spriteData.TextureImporter.spriteImportMode != SpriteImportMode.Single)
                return false;
            
            if (!TryFindSmallerSizeTexture(spriteData, out int smallerSize)) 
                return false;
            
            return spriteData.TextureImporter.maxTextureSize != smallerSize;
        }
    }
}