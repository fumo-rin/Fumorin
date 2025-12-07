using UnityEngine;
using System.Collections.Generic;

namespace RinCore
{
    static partial class Helper
    {
        static Dictionary<int, WaitForSeconds> wfsCache;

        [Initialize(-99999)]
        private static void ResetWaitforsecondsCache()
        {
            wfsCache = new Dictionary<int, WaitForSeconds>();
        }

        public static WaitForSeconds WaitForSeconds(this float seconds, bool cached = true)
        {
            if (seconds <= 0f)
            {
                return null;
            }

            if (!cached)
            {
                return new WaitForSeconds(seconds);
            }
            int msKey = Mathf.RoundToInt(seconds * 1000f);

            if (wfsCache.TryGetValue(msKey, out WaitForSeconds value))
            {
                return value;
            }

            WaitForSeconds spawned = new WaitForSeconds(seconds);
            wfsCache[msKey] = spawned;
            return spawned;
        }

        public static WaitForSeconds WaitForSeconds(this float seconds, ref float totalElapsed)
        {
            totalElapsed += seconds;
            return seconds.WaitForSeconds();
        }
    }
}
