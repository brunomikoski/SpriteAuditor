namespace UnityEngine
{
    public static partial class SpriteMaskExtensions
    {
        public static Vector2 GetPixelSize(this SpriteMask spriteMask, Camera camera)
        {
            Sprite sprite = spriteMask.sprite;
            Vector2 spriteSize = spriteMask.bounds.size;
            Vector2 localSpriteSize = spriteSize / sprite.pixelsPerUnit;
            Vector3 worldSize = localSpriteSize;
            Vector3 lossyScale = spriteMask.transform.lossyScale;
            worldSize.x *= lossyScale.x;
            worldSize.y *= lossyScale.y;
 
            Vector3 screenSize = 0.5f * worldSize / camera.orthographicSize;
            screenSize.y *= camera.aspect;
 
            Vector3 inPixels = new Vector3(screenSize.x * camera.pixelWidth, screenSize.y * camera.pixelHeight, 0) * 0.5f;
            return inPixels;
        }
    }
}