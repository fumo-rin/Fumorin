using UnityEngine;

namespace rinCore
{
    public sealed class RollingFloatTracker
    {
        private struct Bucket
        {
            public float time;
            public float value;
        }

        private readonly Bucket[] buckets;

        public readonly float interval;
        public readonly float window;

        private readonly float emaAlpha;

        private float runningTotal;
        private float ema;
        private float lastRunningTotal;

        private float firstSampleTime = -1f;
        private float lastSampleTime = -1f;

        public RollingFloatTracker(float windowSeconds, float intervalSeconds, float emaSmoothing = 25f)
        {
            window = Mathf.Max(0.01f, windowSeconds);
            interval = Mathf.Max(0.01f, intervalSeconds);
            emaAlpha = Mathf.Max(0.01f, emaSmoothing);

            int count = Mathf.CeilToInt(window / interval);
            buckets = new Bucket[count];
        }
        private int GetIndex(float time) => Mathf.FloorToInt(time / interval) % buckets.Length;
        private float GetBucketTime(float time) => Mathf.Floor(time / interval) * interval;
        public void Record(float value)
        {
            float now = Time.time;

            if (lastSampleTime > 0f && now - lastSampleTime > window)
            {
                firstSampleTime = now;
                ema = 0f;
                lastRunningTotal = runningTotal;
            }
            if (firstSampleTime < 0f)
                firstSampleTime = now;
            lastSampleTime = now;
            int index = GetIndex(now);
            float bucketTime = GetBucketTime(now);
            ref Bucket b = ref buckets[index];
            if (b.time != bucketTime)
            {
                if (now - b.time <= window)
                {
                    runningTotal -= b.value;
                }
                b.value = 0f;
                b.time = bucketTime;
            }
            b.value += value;
            runningTotal += value;
        }
        public static RollingFloatTracker operator +(RollingFloatTracker tracker, float value)
        {
            tracker.Record(value);
            return tracker;
        }
        public float Total
        {
            get
            {
                ExpireOldBuckets();
                return runningTotal;
            }
        }
        private float SampleDuration
        {
            get
            {
                if (firstSampleTime < 0f)
                    return 0f;

                return Mathf.Clamp(
                    Time.time - firstSampleTime,
                    0f,
                    window);
            }
        }
        private float SampleQuality01
        {
            get
            {
                return Mathf.Clamp01(
                    SampleDuration / (window * 0.5f));
            }
        }
        public float PerSecond
        {
            get
            {
                ExpireOldBuckets();

                float duration = Mathf.Max(
                    SampleDuration,
                    interval);

                return runningTotal / duration;
            }
        }
        public float EMA_PerSecond
        {
            get
            {
                float t = SampleQuality01;
                return Mathf.Lerp(ema, PerSecond, t);
            }
        }
        public void TickEMA()
        {
            ExpireOldBuckets();

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);

            float delta = runningTotal - lastRunningTotal;
            lastRunningTotal = runningTotal;

            float instantaneousDps = delta / dt;

            float alpha = 1f - Mathf.Exp(-emaAlpha * dt);

            ema = Mathf.Lerp(
                ema,
                instantaneousDps,
                alpha);
        }
        private void ExpireOldBuckets()
        {
            float now = Time.time;

            for (int i = 0; i < buckets.Length; i++)
            {
                ref Bucket b = ref buckets[i];

                if (b.value == 0f)
                    continue;

                if (now - b.time > window)
                {
                    runningTotal -= b.value;
                    b.value = 0f;
                    b.time = 0f;
                }
            }

            if (runningTotal <= 0.0001f)
            {
                runningTotal = 0f;
            }
        }
        public void Clear()
        {
            runningTotal = 0f;
            ema = 0f;
            lastRunningTotal = 0f;

            firstSampleTime = -1f;
            lastSampleTime = -1f;

            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i].value = 0f;
                buckets[i].time = 0f;
            }
        }
    }
}