using System;

public static class LerpCurves
{
    public static float Linear(float t) => Clamp01(t);

    public static float EaseInQuad(float t)
    {
        t = Clamp01(t);
        return t * t;
    }

    public static float EaseOutQuad(float t)
    {
        t = Clamp01(t);
        return t * (2f - t);
    }

    public static float EaseInOutQuad(float t)
    {
        t = Clamp01(t);
        return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
    }

    public static float EaseInCubic(float t)
    {
        t = Clamp01(t);
        return t * t * t;
    }

    public static float EaseOutCubic(float t)
    {
        t = Clamp01(t) - 1f;
        return t * t * t + 1f;
    }

    public static float EaseInOutCubic(float t)
    {
        t = Clamp01(t);
        return t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;
    }

    public static float EaseInQuart(float t)
    {
        t = Clamp01(t);
        return t * t * t * t;
    }

    public static float EaseOutQuart(float t)
    {
        t = Clamp01(t) - 1f;
        return 1f - t * t * t * t;
    }

    public static float EaseInOutQuart(float t)
    {
        t = Clamp01(t);
        return t < 0.5f ? 8f * t * t * t * t : 1f - 8f * (t - 1f) * (t - 1f) * (t - 1f) * (t - 1f);
    }

    public static float EaseInQuint(float t)
    {
        t = Clamp01(t);
        return t * t * t * t * t;
    }

    public static float EaseOutQuint(float t)
    {
        t = Clamp01(t) - 1f;
        return 1f + t * t * t * t * t;
    }

    public static float EaseInOutQuint(float t)
    {
        t = Clamp01(t);
        return t < 0.5f ? 16f * t * t * t * t * t : 1f + 16f * (t - 1f) * (t - 1f) * (t - 1f) * (t - 1f) * (t - 1f);
    }

    public static float EaseInSine(float t)
    {
        t = Clamp01(t);
        return 1f - (float)Math.Cos(t * Math.PI / 2f);
    }

    public static float EaseOutSine(float t)
    {
        t = Clamp01(t);
        return (float)Math.Sin(t * Math.PI / 2f);
    }

    public static float EaseInOutSine(float t)
    {
        t = Clamp01(t);
        return -(float)(Math.Cos(Math.PI * t) - 1f) / 2f;
    }

    public static float EaseInExpo(float t)
    {
        t = Clamp01(t);
        return (t == 0f) ? 0f : (float)Math.Pow(2f, 10f * (t - 1f));
    }

    public static float EaseOutExpo(float t)
    {
        t = Clamp01(t);
        return (t == 1f) ? 1f : 1f - (float)Math.Pow(2f, -10f * t);
    }

    public static float EaseInOutExpo(float t)
    {
        t = Clamp01(t);
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        if (t < 0.5f) return (float)(Math.Pow(2, 20 * t - 10) / 2);
        return (float)((2 - Math.Pow(2, -20 * t + 10)) / 2);
    }

    public static float EaseInCirc(float t)
    {
        t = Clamp01(t);
        return 1f - (float)Math.Sqrt(1f - t * t);
    }

    public static float EaseOutCirc(float t)
    {
        t = Clamp01(t) - 1f;
        return (float)Math.Sqrt(1f - t * t);
    }

    public static float EaseInOutCirc(float t)
    {
        t = Clamp01(t);
        if (t < 0.5f) return (1f - (float)Math.Sqrt(1f - 4f * t * t)) / 2f;
        else
        {
            t = 2f * t - 2f;
            return ((float)Math.Sqrt(1f - t * t) + 1f) / 2f;
        }
    }

    public static float EaseInBack(float t)
    {
        t = Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }

    public static float EaseOutBack(float t)
    {
        t = Clamp01(t) - 1f;
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }

    public static float EaseInOutBack(float t)
    {
        t = Clamp01(t);
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;

        if (t < 0.5f)
            return (float)(Math.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2f;
        else
        {
            t = 2f * t - 2f;
            return (float)(Math.Pow(t, 2) * ((c2 + 1) * t + c2) + 2f) / 2f;
        }
    }

    public static float EaseInElastic(float t)
    {
        t = Clamp01(t);
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        float c4 = (2f * (float)Math.PI) / 3f;
        return -(float)Math.Pow(2f, 10f * t - 10f) * (float)Math.Sin((t * 10f - 10.75f) * c4);
    }

    public static float EaseOutElastic(float t)
    {
        t = Clamp01(t);
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        float c4 = (2f * (float)Math.PI) / 3f;
        return (float)Math.Pow(2f, -10f * t) * (float)Math.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    public static float EaseInOutElastic(float t)
    {
        t = Clamp01(t);
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        float c5 = (2f * (float)Math.PI) / 4.5f;

        if (t < 0.5f)
            return -0.5f * (float)Math.Pow(2f, 20f * t - 10f) * (float)Math.Sin((20f * t - 11.125f) * c5);
        else
            return (float)Math.Pow(2f, -20f * t + 10f) * (float)Math.Sin((20f * t - 11.125f) * c5) * 0.5f + 1f;
    }

    public static float EaseOutBounce(float t)
    {
        t = Clamp01(t);
        float n1 = 7.5625f;
        float d1 = 2.75f;

        if (t < 1f / d1)
        {
            return n1 * t * t;
        }
        else if (t < 2f / d1)
        {
            t -= 1.5f / d1;
            return n1 * t * t + 0.75f;
        }
        else if (t < 2.5f / d1)
        {
            t -= 2.25f / d1;
            return n1 * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }

    public static float EaseInBounce(float t)
    {
        t = Clamp01(t);
        return 1f - EaseOutBounce(1f - t);
    }

    public static float EaseInOutBounce(float t)
    {
        t = Clamp01(t);
        if (t < 0.5f)
            return (1f - EaseOutBounce(1f - 2f * t)) * 0.5f;
        else
            return (1f + EaseOutBounce(2f * t - 1f)) * 0.5f;
    }

    private static float Clamp01(float t)
    {
        if (t < 0f) return 0f;
        if (t > 1f) return 1f;
        return t;
    }
}
