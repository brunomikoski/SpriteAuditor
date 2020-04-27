using UnityEngine;
using UnityEngine.UI;

namespace BrunoMikoski.AtlasAudior
{
    public class AtlasAuditorSpriteDetector : IProjectUpdateLoopListener
    {
        private AtlasAuditorResult result;

        public void SetResult(AtlasAuditorResult targetResult)
        {
            result = targetResult;
        }

        void IProjectUpdateLoopListener.OnProjectUpdate()
        {
            if (result == null)
                return;
            
            Image[] images = Object.FindObjectsOfType<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                result.AddSprite(image.sprite, image.gameObject);
            }

            SpriteRenderer[] spriteRenderers = Object.FindObjectsOfType<SpriteRenderer>();
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                result.AddSprite(spriteRenderer.sprite, spriteRenderer.gameObject);
            }
            
            SpriteMask[] spriteMasks = Object.FindObjectsOfType<SpriteMask>();
            for (int i = 0; i < spriteMasks.Length; i++)
            {
                SpriteMask spriteMask = spriteMasks[i];
                result.AddSprite(spriteMask.sprite, spriteMask.gameObject);
            }
            
            Button[] buttons = Object.FindObjectsOfType<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button.targetGraphic is Image targetGraphicImage)
                    result.AddSprite(targetGraphicImage.sprite, button.gameObject);

                result.AddSprite(button.spriteState.disabledSprite, button.gameObject);
                result.AddSprite(button.spriteState.highlightedSprite, button.gameObject);
                result.AddSprite(button.spriteState.pressedSprite, button.gameObject);
            }
        }
    }
}