using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace BrunoMikoski.SpriteAuditor
{
    public sealed class SpriteResultView : BaseResultView
    {
        [Flags]
        private enum Filter
        {
            WithSizeWarnings = 1 << 0,
            UsedSmallerThanSpriteSize = 1 << 1,
            UsedBiggerThanSpriteSize = 1 << 2,
            UsedInOneScene = 1 << 3,
            UsedInMoreThanOneScene = 1 << 4,
            InsideAnyAtlas = 1 << 5,
        }

        private SpriteData[] usedSpriteDatas = new SpriteData[0];
        private Sprite[] unusedSprites = new Sprite[0];
        private Filter currentFilter;

        public override void GenerateResults(SpriteDatabase spriteDatabase)
        {
            if (spriteDatabase?.SpritesData == null)
                return;

            string[] allTextures2DGUIDs = AssetDatabase.FindAssets("t:Texture2D");
            List<SpriteData> usedSpriteDatas = new List<SpriteData>();
            List<Sprite> unusedSprites = new List<Sprite>();

            foreach (string textureGUIDs in allTextures2DGUIDs)
            {
                IEnumerable<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(textureGUIDs))
                    .Where(o => o is Sprite).Cast<Sprite>();

                foreach (Sprite sprite in sprites)
                {
                    if (spriteDatabase.TryGetSpriteDataBySprite(sprite, out SpriteData spriteData))
                    {
                        if (!MatchFilter(spriteData))
                            continue;
                        
                        usedSpriteDatas.Add(spriteData);
                    }
                    else
                    {
                        if(!MatchFilter(sprite))
                            continue;
                        unusedSprites.Add(sprite);
                    }
                }
            }

            this.unusedSprites = unusedSprites.ToArray();
            this.usedSpriteDatas = usedSpriteDatas.ToArray();
        }

        private bool MatchFilter(Sprite spriteData)
        {
            if (currentFilter.HasFlag(Filter.WithSizeWarnings))
                return false;

            if (currentFilter.HasFlag(Filter.UsedInOneScene))
                return false;

            if (currentFilter.HasFlag(Filter.UsedBiggerThanSpriteSize))
                return false;
            
            if (currentFilter.HasFlag(Filter.UsedSmallerThanSpriteSize))
                return false;
            
            if (currentFilter.HasFlag(Filter.UsedInMoreThanOneScene))
                return false;

            if (currentFilter.HasFlag(Filter.InsideAnyAtlas))
            {
                if (AtlasCacheUtility.TryGetAtlasForSprite(spriteData, out SpriteAtlas atlas))
                    return true;
                return false;
            }

            return true;
        }

        private bool MatchFilter(SpriteData spriteData)
        {
            if (currentFilter.HasFlag(Filter.WithSizeWarnings))
            {
                if (!spriteData.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsedBiggerThanSpriteRect)
                    && !spriteData.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsedSmallerThanSpriteRect))
                {
                    return false;
                }
            }

            if (currentFilter.HasFlag(Filter.UsedInOneScene))
            {
                if(spriteData.SceneAssets.Count != 1)
                   return false;
            }

            if (currentFilter.HasFlag(Filter.UsedBiggerThanSpriteSize))
            {
                if (!spriteData.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsedBiggerThanSpriteRect))
                    return false;
            }

            if (currentFilter.HasFlag(Filter.UsedSmallerThanSpriteSize))
            {
                if (!spriteData.SpriteUsageFlags.HasFlag(SpriteUsageFlags.UsedSmallerThanSpriteRect))
                    return false;
            }

            if (currentFilter.HasFlag(Filter.UsedInMoreThanOneScene))
            {
                if (spriteData.SceneAssets.Count <= 1)
                    return false;
            }

            if (currentFilter.HasFlag(Filter.InsideAnyAtlas))
            {
                return spriteData.SpriteAtlas != null;
            }

            return true;
        }

        public override void DrawFilterOptions()
        {
            EditorGUI.BeginChangeCheck();
            currentFilter = (Filter) EditorGUILayout.EnumFlagsField("Filter", currentFilter);
            if (EditorGUI.EndChangeCheck())
                SpriteAuditorUtility.SetResultViewDirty();
        }

        public override void DrawResults(SpriteDatabase spriteDatabase)
        {
            EditorGUILayout.BeginVertical("Box");
            if (usedSpriteDatas.Length > 0)
            {
                if (SpriteAuditorGUIUtility.DrawStringFoldout(
                    $"Used Sprites [{usedSpriteDatas.Length}] ",
                    $"{VisualizationType.Sprite.ToString()}_used_sprites"))
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < usedSpriteDatas.Length; i++)
                    {
                        SpriteData usedSpriteData = usedSpriteDatas[i];
                        DrawSpriteDataField(usedSpriteData);
                    }

                    EditorGUI.indentLevel--;
                }
            }

            if (unusedSprites.Length > 0)
            {
                if (SpriteAuditorGUIUtility.DrawStringFoldout(
                    $"Unused Sprites [{unusedSprites.Length}] ",
                    $"{VisualizationType.Sprite.ToString()}_unused_sprites"))
                {
                    for (int i = 0; i < unusedSprites.Length; i++)
                    {
                        Sprite sprite = unusedSprites[i];
                        SpriteAuditorGUIUtility.DrawObjectFoldout(sprite,
                            $"{VisualizationType.Sprite.ToString()}_{sprite.name}", false);

                    }
                }
            }

            EditorGUILayout.EndVertical();

            
        }
    }
}