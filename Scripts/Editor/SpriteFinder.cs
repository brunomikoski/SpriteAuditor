using UnityEngine;
using UnityEngine.UI;

namespace BrunoMikoski.SpriteAuditor
{
    public class SpriteFinder : IProjectUpdateLoopListener
    {
        private SpriteDatabase result;
        private int frameRecordInterval = 1;
        private int currentFrameCount = 0;
        private bool recordOnUpdate = true;

        public void SetResult(SpriteDatabase targetResult)
        {
            result = targetResult;
        }

        void IProjectUpdateLoopListener.OnProjectAwake()
        {
            ResetFrameCount();
        }

        private void ResetFrameCount()
        {
            currentFrameCount = frameRecordInterval;
        }
        
        public void SetFrameInterval(int frameInterval)
        {
            frameRecordInterval = frameInterval;
        }

        void IProjectUpdateLoopListener.OnProjectUpdate()
        {
            if (!recordOnUpdate)
                return;
            
            currentFrameCount--;

            if (currentFrameCount > 0)
                return;
            
            ResetFrameCount();
            
            CaptureFrame();
        }

        public void CaptureFrame()
        {
            Component[] components = Object.FindObjectsOfType<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                if (component is Image image)
                {
                    result.ReportImage(image);
                }
                else if (component is Button button)
                {
                    result.ReportButton(button);
                }
                else if (component is SpriteRenderer spriteRenderer)
                {
                    result.ReportSpriteRenderer(spriteRenderer);
                }
                else if (component is SpriteMask spriteMask)
                {
                    result.ReportSpriteMask(spriteMask);
                }
            }
        }

        public void SetCaptureOnUpdate(bool recordOnUpdate)
        {
            this.recordOnUpdate = recordOnUpdate;
        }
    }
}
