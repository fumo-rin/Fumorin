using UnityEngine;

namespace RinCore
{
    public static class SpriteExtensions
    {
        public static float GetHeight(this SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return 1f;
            }

            Vector2 spriteSize = renderer.sprite.bounds.size;
            Vector3 worldSize = Vector3.Scale(spriteSize, renderer.transform.lossyScale);
            return worldSize.y;
        }
        public static float GetWidth(this SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return 1f;
            }

            Vector2 spriteSize = renderer.sprite.bounds.size;
            Vector3 worldSize = Vector3.Scale(spriteSize, renderer.transform.lossyScale);
            return worldSize.x;
        }
        public static void SetHeight(this SpriteRenderer renderer, float targetHeight)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }
            Vector2 spriteSize = renderer.sprite.bounds.size;
            Vector3 worldSize = Vector3.Scale(spriteSize, renderer.transform.lossyScale);
            float scaleFactor = targetHeight / worldSize.y;
            renderer.transform.localScale *= scaleFactor;
        }
        public static void SetWidth(this SpriteRenderer renderer, float targetWidth)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }
            Vector2 spriteSize = renderer.sprite.bounds.size;
            Vector3 worldSize = Vector3.Scale(spriteSize, renderer.transform.lossyScale);
            float scaleFactor = targetWidth / worldSize.x;
            renderer.transform.localScale *= scaleFactor;
        }
    }
}
