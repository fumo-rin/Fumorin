using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    public static class ImageExtensions
    {
        public static void Opacity_Nullsafe(this Image i, byte a)
        {
            if (i == null)
            {
                return;
            }
            i.color = i.color.Opacity(a);
        }
    }
}
