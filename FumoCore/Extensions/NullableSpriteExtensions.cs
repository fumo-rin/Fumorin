using UnityEngine;

namespace FumoCore.Tools
{
    public static class NullableSpriteExtensions
    {
        public static void SetEnabledNullable(this SpriteRenderer sr, bool state)
        {
            NSEHelper.SetNullableSpriteActive(sr, state);
        }
        private static class NSEHelper
        {
            public static void SetNullableSpriteActive(SpriteRenderer sr, bool state)
            {
                if (sr == null)
                {
                    return;
                }
                sr.enabled = state;
            }
        }
    }
}
