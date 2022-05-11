using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace BrunoMikoski.SpriteAuditor
{
    public sealed class AtlasResultView : BaseResultView
    {
        [Flags]
        public enum Filter
        {
            IncludedInBuild = 1 << 0,
            IsVariant = 1 << 1,
            WithUsagesFound = 1 << 2
        }

        private Filter currentFilter;
        
        private SpriteAtlas[] filteredAtlas = new SpriteAtlas[0];
        private Dictionary<SpriteAtlas, SpriteData[]> atlasToUsedSprites = new Dictionary<SpriteAtlas, SpriteData[]>();
        private Dictionary<SpriteAtlas, Sprite[]> atlasToNotUsedSprites = new Dictionary<SpriteAtlas, Sprite[]>();

        public override void DrawFilterOptions()
        {
            EditorGUI.BeginChangeCheck();
            currentFilter = (Filter) EditorGUILayout.EnumFlagsField("Filter", currentFilter);
            if (EditorGUI.EndChangeCheck())
                SpriteAuditorUtility.SetResultViewDirty();
        }

        public override void GenerateResults(SpriteDatabase spriteDatabase)
        {
            if (spriteDatabase?.SpritesData == null)
                return;

            atlasToUsedSprites.Clear();
            atlasToNotUsedSprites.Clear();

            filteredAtlas = AtlasCacheUtility.GetAllKnowAtlases()
                .Where(atlas => MatchFilter(atlas, spriteDatabase))
                .OrderBy(atlas => atlas.name).ToArray();
            
            SpriteData[] validSprites = spriteDatabase.SpritesData.Where(ValidSpriteData).Where(data => data.IsInsideAtlas())
                .OrderBy(data => data.Sprite.name).ToArray();

            for (int i = 0; i < filteredAtlas.Length; i++)
            {
                SpriteAtlas atlas = filteredAtlas[i];

                SpriteData[] usedSpritesFromThisAtlas =
                    validSprites.Where(data =>
                        data.IsInsideAtlas() && data.SpriteAtlas == atlas && MatchSearch(data.Sprite.name)).ToArray();
                atlasToUsedSprites.Add(atlas, usedSpritesFromThisAtlas);
                Sprite[] spritesInsideAtlas = AtlasCacheUtility.GetAllSpritesFromAtlas(atlas)
                    .Where(sprite => MatchSearch(sprite.name)).ToArray();

                Sprite[] notUSedSprites =
                    spritesInsideAtlas.Where(sprite => usedSpritesFromThisAtlas.All(data => data.Sprite != sprite)).ToArray();
                
                atlasToNotUsedSprites.Add(atlas, notUSedSprites);
            }
        }

        protected override void DrawResultsInternal(SpriteDatabase spriteDatabase)
        {
            for (int i = 0; i < filteredAtlas.Length; i++)
            {
                SpriteAtlas atlas = filteredAtlas[i];
                
                EditorGUILayout.BeginVertical("Box");

                if (SpriteAuditorGUIUtility.DrawObjectFoldout(atlas,
                    $"{VisualizationType.Atlas.ToString()}_{atlas.name}"))
                {
                    EditorGUI.indentLevel++;

                    if (atlasToUsedSprites[atlas].Length > 0)
                    {
                        if (SpriteAuditorGUIUtility.DrawStringFoldout(
                            $"Used Sprites [{atlasToUsedSprites[atlas].Length}] ",
                            $"{VisualizationType.Atlas.ToString()}_{atlas.name}_used_sprites"))
                        {
                            EditorGUI.indentLevel++;

                            for (int j = 0; j < atlasToUsedSprites[atlas].Length; j++)
                            {
                                SpriteData spriteData = atlasToUsedSprites[atlas][j];
                                DrawSpriteDataField(spriteData);
                            }

                            SpriteAuditorUtility.DrawDefaultSelectionOptions(atlasToNotUsedSprites[atlas]);

                            EditorGUI.indentLevel--;
                        }
                    }

                    if (atlasToNotUsedSprites[atlas].Length > 0)
                    {
                        if (SpriteAuditorGUIUtility.DrawStringFoldout(
                            $"Not Used Sprites [{atlasToNotUsedSprites[atlas].Length}] ",
                            $"{VisualizationType.Atlas.ToString()}_{atlas.name}_not_used_sprites"))
                        {
                            EditorGUI.indentLevel++;

                            for (int j = 0; j < atlasToNotUsedSprites[atlas].Length; j++)
                            {
                                Sprite sprite = atlasToNotUsedSprites[atlas][j];
                                if (sprite == null)
                                    continue;

                                SpriteAuditorGUIUtility.DrawObjectFoldout(sprite,
                                    $"{VisualizationType.Atlas.ToString()}_{atlas.name}_{sprite.name}", false, true);
                            }

                            SpriteAuditorUtility.DrawDefaultSelectionOptions(atlasToNotUsedSprites[atlas]);
                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
        }

        
        private bool MatchFilter(SpriteAtlas atlas, SpriteDatabase spriteDatabase)
        {
            
            if (currentFilter.HasFlag(Filter.IncludedInBuild))
            {
                if (!atlas.IsIncludedInBuild())
                    return false;
            }

            if (currentFilter.HasFlag(Filter.IsVariant))
            {
                if (!atlas.isVariant)
                    return false;
            }

            if (currentFilter.HasFlag(Filter.WithUsagesFound))
            {
                for (int i = 0; i < spriteDatabase.SpritesData.Count; i++)
                {
                    SpriteData spriteData = spriteDatabase.SpritesData[i];
                    if (!spriteData.IsInsideAtlas())
                        continue;

                    if (spriteData.SpriteAtlas == atlas)
                        return true;
                }
                return false;
            }

            return true;
        }
    }
}
