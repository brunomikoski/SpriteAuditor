namespace UnityEngine
{
    public static partial class SpriteRendererExtensions
    {
        public static Vector2 GetPixelSize(this SpriteRenderer spriteRenderer, Camera camera = null)
        {
            if (spriteRenderer == null) return Vector2.zero;

            if (spriteRenderer.sprite == null) return Vector2.zero;

            float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;

            float offsetRight = spriteRenderer.sprite.rect.size.x / 2f / pixelsPerUnit;
            float offsetUp = spriteRenderer.sprite.rect.size.y / 2f / pixelsPerUnit;

            Vector2 localRight = Vector2.right * offsetRight;
            Vector2 localUp = Vector2.up * offsetUp;

            Vector2 worldRight = spriteRenderer.transform.TransformPoint(localRight);
            Vector2 worldUp = spriteRenderer.transform.TransformPoint(localUp);
            Vector2 worldCenter = spriteRenderer.transform.position;

            Vector2 coordsRight = GetPixelCoordinates(worldRight, camera);
            Vector2 coordsUp = GetPixelCoordinates(worldUp, camera);
            Vector2 coordsCenter = GetPixelCoordinates(worldCenter, camera);

            float pixelsRight = Vector2.Distance(coordsCenter, coordsRight);
            float pixelsUp = Vector2.Distance(coordsCenter, coordsUp);

            Vector2 itemSize = Vector2.right * pixelsRight * 2 + Vector2.up * pixelsUp * 2;

            return itemSize;
        }

        private static Vector2 GetPixelCoordinates(Vector3 position, Camera camera)
        {
            if (camera == null)
                camera = Camera.main;

            if (camera == null) return Vector2.zero;

            return camera.WorldToScreenPoint(position);
        }
    }
}