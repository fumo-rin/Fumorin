using UnityEngine;
using System.Collections.Generic;

namespace RinCore
{
    static partial class RinHelper
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
                return new UnityEngine.WaitForSeconds(seconds);
            }
            if (wfsCache.Count > 10000)
            {
                wfsCache.Clear();
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
    }
}
