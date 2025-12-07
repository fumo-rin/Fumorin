using UnityEngine;

namespace FumoCore.Tools
{
    public static class Collider2DExtension
    {
        public static bool TryBoxcastBottom(this BoxCollider2D collider, float distance, LayerMask layerMask, out RaycastHit2D hit)
        {
            hit = default;
            if (collider == null) return false;

            Vector2 worldCenter = collider.transform.TransformPoint(collider.offset);
            Vector2 worldSize = Vector2.Scale(collider.size, collider.transform.lossyScale);

            Vector2 boxSize = new Vector2(worldSize.x * 0.95f, worldSize.y * 0.1f);

            Vector2 origin = worldCenter + Vector2.down * (worldSize.y * 0.5f - boxSize.y * 0.5f);

            hit = Physics2D.BoxCast(
                origin,
                boxSize,
                collider.transform.eulerAngles.z,
                Vector2.down,
                distance,
                layerMask
            );

            return hit.collider != null;
        }
    }
}
