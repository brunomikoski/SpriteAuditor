using UnityEngine;

namespace UnityEngine
{
    public static class SpriteRendererExtensions
    {
         public static Vector2 GetPixelSize(this SpriteRenderer spriteRenderer, Camera camera = null)
            {
                if (spriteRenderer == null) return Vector2.zero;

                if (spriteRenderer.sprite == null) return Vector2.zero;

                float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;

                // Get top left corner
                float offsetRight = spriteRenderer.sprite.rect.size.x / 2f / pixelsPerUnit;
                float offsetUp = spriteRenderer.sprite.rect.size.y / 2f / pixelsPerUnit;

                Vector2 localRight = Vector2.right * offsetRight;
                Vector2 localUp = Vector2.up * offsetUp;

                // Go to world
                Vector2 worldRight = spriteRenderer.transform.TransformPoint(localRight);
                Vector2 worldUp = spriteRenderer.transform.TransformPoint(localUp);
                Vector2 worldCenter = spriteRenderer.transform.position;

                // Go to pixels
                Vector2 coordsRight = GetPixelCoordinates(worldRight, camera);
                Vector2 coordsUp = GetPixelCoordinates(worldUp, camera);
                Vector2 coordsCenter = GetPixelCoordinates(worldCenter, camera);

                // Get sizes
                float pixelsRight = Vector2.Distance(coordsCenter, coordsRight);
                float pixelsUp = Vector2.Distance(coordsCenter, coordsUp);

                Vector2 itemSize = Vector2.right * pixelsRight * 2 + Vector2.up * pixelsUp * 2;

                return itemSize;
            }

            public static Vector2 GetPixelCoordinates(this Transform transform, Camera camera = null)
            {
                if (transform == null) return Vector2.zero;

                return GetPixelCoordinates(transform.position, camera);
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