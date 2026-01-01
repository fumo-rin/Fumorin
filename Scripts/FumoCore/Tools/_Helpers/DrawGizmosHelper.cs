using UnityEngine;

namespace RinCore
{
    public static partial class RinHelper
    {
        public static void GizmosDrawCircle(Vector2 position, float radius, Color32 color, byte fillOpacity = 0)
        {
            Gizmos.color = fillOpacity <= 0f ? color.Opacity(255) : color.Opacity(fillOpacity);

            if (fillOpacity > 0f)
            {
                Gizmos.DrawSphere(position, radius);
            }
            else
            {
                Gizmos.DrawWireSphere(position, radius);
            }
        }
    }
}
