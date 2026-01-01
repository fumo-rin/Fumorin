using UnityEngine;
using System.Collections.Generic;

namespace RinCore
{
    public class WaitForSecondsCached : CustomYieldInstruction
    {
        readonly float duration;
        float startTime;
        public WaitForSecondsCached(float seconds)
        {
            duration = seconds;
            Reset();
        }
        public override void Reset()
        {
            startTime = Time.time;
        }
        public override bool keepWaiting =>
            Time.time - startTime < duration;
    }
    static partial class RinHelper
    {
        static Dictionary<int, WaitForSecondsCached> wfsCache;

        [Initialize(-99999)]
        private static void ResetWaitforsecondsCache()
        {
            wfsCache = new Dictionary<int, WaitForSecondsCached>();
        }
        public static WaitForSecondsCached WaitForSeconds(this float seconds, bool cached = true)
        {
            if (seconds <= 0f)
            {
                return null;
            }

            if (!cached)
            {
                return new WaitForSecondsCached(seconds);
            }
            if (wfsCache.Count > 10000)
            {
                wfsCache.Clear();
            }
            int msKey = Mathf.RoundToInt(seconds * 1000f);

            if (wfsCache.TryGetValue(msKey, out WaitForSecondsCached value))
            {
                return value;
            }

            WaitForSecondsCached spawned = new WaitForSecondsCached(seconds);
            wfsCache[msKey] = spawned;
            return spawned;
        }
        public static WaitForSecondsCached WaitForSeconds(this float seconds, ref float totalElapsed)
        {
            totalElapsed += seconds;
            return seconds.WaitForSeconds();
        }
    }
}
