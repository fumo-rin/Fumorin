using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FumoCore.Tools
{
    public static class FloatExtensions
    {
        public static readonly float Percent = 0.01f;
        public static float LerpTime(this float f, float target, float delta)
        {
            return Mathf.Lerp(f, target, delta * Time.deltaTime);
        }
        public static float LerpUnclamped(this float f, float target, float lerp)
        {
            return f + (target - f) * lerp;
        }
        public static float MapToLerp(this float lerp, float a, float b)
        {
            return a + (b - a) * lerp;
        }
        public static float Clamp(this float f, float min, float max)
        {
            return Mathf.Clamp(f, min, max);
        }
        public static float Clamp(this float f, Vector2 range)
        {
            return Clamp(f, range.x, range.y);
        }
        public static float AboveOrZero(this float f)
        {
            return Mathf.Max(f, 0f);
        }
        public static bool IsBelowOrZero(this float f)
        {
            return f <= 0f;
        }
        public static float Round(this float f)
        {
            return Mathf.Round(f);
        }
        public static float Min(this float f, float min)
        {
            return Mathf.Min(f, min);
        }
        public static float Max(this float f, float max)
        {
            return Mathf.Max(f, max);
        }
        public static float Absolute(this float f)
        {
            return Mathf.Abs(f);
        }
        public static float AbsoluteNegative(this float f)
        {
            return 0f - f.Absolute();
        }
        public static float AbsoluteBetween(this float f, float target)
        {
            return Mathf.Abs(f - target);
        }
        public static float Spread(this float f, float percentage = 5f)
        {
            if (percentage < 1f)
                return f;
            return f * UnityEngine.Random.Range(1 - percentage.Clamp(0f, 100f) * Percent, 1 + percentage.Clamp(0f, 100f) * Percent);
        }
        public static float Sign(this float value) => value > 0f ? 1f : value < 0f ? -1f : 0f;
        public static int SignInt(this float f)
        {
            return (int)Mathf.Sign(f);
        }
        public static float ToFloat(this bool b, float trueValue = 1f, float falseValue = 0f)
        {
            return b ? trueValue : falseValue;
        }
        public static float AddRandomBetween(this float f, float min, float max)
        {
            return f + UnityEngine.Random.Range(min, max);
        }
        public static int ToInt(this float f)
        {
            return Mathf.FloorToInt(f);
        }
        public static byte ToByte(this float f)
        {
            return (byte)Mathf.FloorToInt(f);
        }
        public static float Multiply(this float f, float value)
        {
            return f = f * value;
        }
        public static float Half(this float f)
        {
            return f * 0.5f;
        }
        public static float Double(this float f)
        {
            return f * 2f;
        }
        public static float Percentify(this float f)
        {
            return f * 0.01f;
        }
        public static float Ceil(this float f)
        {
            return Mathf.Ceil(f);
        }
        public static float Floor(this float f)
        {
            return Mathf.Floor(f);
        }
        public static bool IsBetween(this float f, float a, float b)
        {
            if (a > b)
            {
                float c = a;
                a = b;
                b = c;
            }
            return f > a && f <= b;
        }
        public static bool IsBetween(this float f, Vector2 range)
        {
            return IsBetween(f, range.x, range.y);
        }
        public static float RandomPositiveNegativeRange(this float f)
        {
            return UnityEngine.Random.Range(-f.Absolute(), f.Absolute());
        }
        public static float Quantize(this float f, float steps, bool roundUp = false)
        {
            if (f == 0f)
            {
                return 0f;
            }
            steps = steps.Max(1f);
            if (roundUp)
            {
                return (Mathf.Ceil(f.Absolute() * steps.Absolute()) / steps.Absolute()) * f.SignInt();
            }
            else
            {
                return (Mathf.Floor(f.Absolute() * steps.Absolute()) / steps.Absolute()) * f.SignInt();
            }
        }
        public static float ReverseQuantize(this float f, float stepSize)
        {
            if (f == 0f)
            {
                return 0f;
            }
            stepSize = stepSize.Max(0.1f);
            float value = Mathf.Floor(f.Absolute() / stepSize.Absolute()) * stepSize.Absolute();
            return value * f.SignInt();
        }
        public static float Squared(this float f) => f * f;
        public static float Mean(this float f, float other)
        {
            return (f + other) * 0.5f;
        }
        public static float AsFloat(this bool b, float trueValue, float falseValue)
        {
            if (b)
            {
                return trueValue;
            }
            return falseValue;
        }
        public static IEnumerable<float> StepFromTo(this float stepSize, float from, float to)
        {
            if (stepSize == 0f)
                yield break;
            stepSize = Mathf.Abs(stepSize) * (from < to ? 1f : -1f);

            if (from < to)
            {
                for (float i = from; i <= to; i += stepSize)
                    yield return i;
            }
            else
            {
                for (float i = from; i >= to; i += stepSize)
                    yield return i;
            }
        }
        public static IEnumerable<float> StepThroughCurve(this AnimationCurve curve, float timeStep, int maximumIterations)
        {
            for (float i = 0f; i <= curve.length || i <= maximumIterations * timeStep; i += timeStep)
            {
                yield return curve.Evaluate(i);
            }
        }
        public static float Duration(this AnimationCurve c)
        {
            return c.keys[c.length - 1].time;
        }
        public static float MoveTowards(this float f, float other, float step)
        {
            return Mathf.MoveTowards(f, other, step);
        }
        public static string ToThousandsString(this float value, int decimals = 0, string thousandsSeparator = ",", CultureInfo culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            NumberFormatInfo nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
            nfi.NumberGroupSeparator = thousandsSeparator;
            string format = "N" + decimals;
            return value.ToString(format, nfi);
        }

        public static string ToThousandsString(this double value, int decimals = 0, string thousandsSeparator = ",", CultureInfo culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            NumberFormatInfo nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
            nfi.NumberGroupSeparator = thousandsSeparator;
            string format = "N" + decimals;
            return value.ToString(format, nfi);
        }
        public static float LerpEaseInOut(this float a, float b, float t)
        {
            t = t.Clamp(0f, 1f);
            t = t * t * (3f - 2f * t);

            return a + (b - a) * t;
        }
        public static float AsEaseInOut01(this float t)
        {
            t = t.Clamp(0f, 1f);
            t = t * t * (3f - 2f * t);
            return t;
        }
        public static float CurveLerp(this float t, float min, float max, float power = 2f)
        {
            float distance = Mathf.Abs(t - 0.5f) * 2f;
            float curve = 1f - (float)Mathf.Pow(distance, power);
            return min + (max - min) * curve;
        }
        public static double Multiply(this double d, double m)
        {
            return d * m;
        }
        public static double Floor(this double d)
        {
            return Math.Floor(d);
        }
        public static double Clamp(this double d, double min, double max)
        {
            if (d < min) return min;
            if (d > max) return max;
            return d;
        }
        public static float OLD_AND_CONFUSING_Sine01(this float time, float frequency = 1f, float amplitude = 1f, float power = 1f)
        {
            float sine = (Mathf.Sin(2f * Mathf.PI * frequency * time) + 1f) * 0.5f;// Normalize to 0–1
            float distance = Mathf.Abs(sine - 0.5f) * 2f;// Distance from center, scaled to 0–1
            float shaped = Mathf.Pow(distance, Mathf.Abs(power));// Apply power shape

            // Restore the direction (above or below 0.5) and center it again
            shaped = 0.5f + Mathf.Sign(sine - 0.5f) * shaped * 0.5f;

            return shaped * amplitude;
        }
        public static float SineWave(this float frequency, float time, float amplitude = 1f)
        {
            float angularFrequency = 2 * (float)Math.PI * frequency;
            return (float)Math.Sin(angularFrequency * time) * amplitude;
        }
        public static float CosWave(this float frequency, float time, float amplitude = 1f)
        {
            float angularFrequency = 2 * (float)Math.PI * frequency;
            return (float)Math.Cos(angularFrequency * time) * amplitude;
        }
        public static float TriangleWave(this float time, float a, float b, float period)
        {
            if (period <= 0f)
            {
                period = 1f;
                Debug.LogWarning("Used 0f for a TriangleWave (Float extension) time period. using 1f instead.");
            }
            float t = (time % period) / period;
            float tri = t < 0.5f ? 2f * t : 2f * (1f - t);
            return Mathf.Lerp(a, b, tri);
        }
        public static long ToLong(this double value, bool round = true)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;

            double result = round ? Math.Round(value) : Math.Truncate(value);

            if (result > long.MaxValue)
                return long.MaxValue;

            if (result < long.MinValue)
                return long.MinValue;

            return (long)result;
        }
    }
}