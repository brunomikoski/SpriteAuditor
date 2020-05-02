using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrunoMikoski.SpriteAuditor
{
    [Serializable]
    public class SpriteDatabase
    {
        [SerializeField]
        private List<SpriteData> spritesData = new List<SpriteData>(512);
        
        private SceneViewResultData sceneViewResultData = new SceneViewResultData();

        public SpriteData[] GetValidSprites()
        {
            return spritesData.Where(data => data.IsValid()).OrderBy(data => data.Sprite.name).ToArray();
        }

        public void ReportButton(Button button)
        {
            Image buttonImage = button.targetGraphic as Image;
            if (buttonImage == null)
                return;

            SpriteData spriteData = GetOrCreateSpriteData(buttonImage.sprite);
            spriteData.ReportUse(buttonImage);

            if (button.spriteState.disabledSprite != null)
            {
                spriteData = GetOrCreateSpriteData(button.spriteState.disabledSprite);
                spriteData.ReportUse(buttonImage);
            }

            if (button.spriteState.highlightedSprite != null)
            {
                spriteData = GetOrCreateSpriteData(button.spriteState.highlightedSprite);
                spriteData.ReportUse(buttonImage);   
            }

            if (button.spriteState.pressedSprite != null)
            {
                spriteData = GetOrCreateSpriteData(button.spriteState.pressedSprite);
                spriteData.ReportUse(buttonImage);
            }
        }

        public void ReportSpriteRenderer(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer.sprite == null)
                return;

            Sprite sprite = spriteRenderer.sprite;
            SpriteData spriteData = GetOrCreateSpriteData(sprite);
            spriteData.ReportUse(spriteRenderer);
        }
        
        public void ReportSpriteMask(SpriteMask spriteMask)
        {
            if (spriteMask.sprite == null)
                return;

            Sprite sprite = spriteMask.sprite;
            SpriteData spriteData = GetOrCreateSpriteData(sprite);
            spriteData.ReportUse(spriteMask);
        }

        
        public void ReportImage(Image image)
        {
            if (image.sprite == null)
                return;

            Sprite sprite = image.sprite;

            SpriteData spriteData = GetOrCreateSpriteData(sprite);

            spriteData.ReportUse(image);
        }

        private SpriteData GetOrCreateSpriteData(Sprite sprite)
        {
            if (TryGetSpriteDataBySprite(sprite, out SpriteData spriteData))
                return spriteData;
            
            spriteData = new SpriteData(sprite);
            spritesData.Add(spriteData);
            return spriteData;
        }

        private bool TryGetSpriteDataBySprite(Sprite sprite, out SpriteData spriteData)
        {
            int spriteDatabaseCount = spritesData.Count;
            for (int i = 0; i < spriteDatabaseCount; i++)
            {
                SpriteData data = spritesData[i];
                if (data.Sprite == sprite)
                {
                    spriteData = data;
                    return true;
                }
            }

            spriteData = null;
            return false;
        }

        public void DrawResults(VisualizationType visualizationType)
        {
            switch (visualizationType)
            {
                case VisualizationType.Scene:
                    sceneViewResultData.DrawResults(this);
                    break;
                case VisualizationType.Atlas:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visualizationType), visualizationType, null);
            }
        }

        public void RefreshResults(VisualizationType visualizationType)
        {
            switch (visualizationType)
            {
                case VisualizationType.Scene:
                    sceneViewResultData.GenerateResults(this);
                    break;
                case VisualizationType.Atlas:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visualizationType), visualizationType, null);
            }
        }
    }
}
