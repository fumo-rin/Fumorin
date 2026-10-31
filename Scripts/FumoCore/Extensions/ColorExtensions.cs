using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace rinCore
{
    public struct ColorHelper
    {
        #region Hierarchy Components
        public struct HierarchyTypes
        {
            public static Color32 Error => ColorHelper.Raw.DeepRed.Opacity(100);
            public static Color32 SessionStarter => new Color32(170, 120, 220, 50);
            public static Color32 VersionText => ColorHelper.Gray.Gray7.Opacity(50);
            public static Color32 UINest => new Color32(210, 130, 190, 50);
        }
        #endregion
        #region Rainbow Time Gradients
        [System.Serializable]
        public struct Rainbow
        {
            [SerializeField] private float frequency, offset, speedMod;
            [SerializeField] private bool isTrans;
            public Rainbow(float frequency, float offset, float speedMod = 1f)
            {
                this.frequency = frequency;
                this.offset = offset;
                this.speedMod = speedMod;
                this.isTrans = false;
            }
            public static implicit operator Color32(Rainbow r)
            {
                return (Color)r;
            }
            public static implicit operator Color(Rainbow r)
            {
                float t = ((Time.time * r.speedMod) + r.offset) / r.frequency;
                if (r.isTrans)
                {
                    float nt = t - Mathf.Floor(t);
                    Color transCyan = new Color(91f / 255f, 206f / 255f, 250f / 255f);
                    Color transPink = new Color(245f / 255f, 169f / 255f, 184f / 255f);

                    if (nt < 0.2f)
                        return Color.Lerp(transCyan, transPink, nt / 0.2f);
                    if (nt < 0.4f)
                        return Color.Lerp(transPink, Color.white, (nt - 0.2f) / 0.2f);
                    if (nt < 0.6f)
                        return Color.Lerp(Color.white, transPink, (nt - 0.4f) / 0.2f);
                    if (nt < 0.8f)
                        return Color.Lerp(transPink, transCyan, (nt - 0.6f) / 0.2f);
                    return Color.Lerp(transCyan, transCyan, (nt - 0.8f) / 0.2f);
                }
                float hue = t - Mathf.Floor(t);
                return Color.HSVToRGB(hue, 1f, 1f);
            }
            public static Rainbow operator *(Rainbow r, float speedMultiplier)
            {
                return new Rainbow(r.frequency, r.offset, r.speedMod * speedMultiplier)
                {
                    isTrans = r.isTrans
                };
            }

            public static Rainbow Rainbow_1sec => new Rainbow(1f, 0f);
            public static Rainbow Rainbow_2sec => new Rainbow(2f, 0.5f);
            public static Rainbow Rainbow_3sec => new Rainbow(3f, 1.2f);
            public static Rainbow Rainbow_3sec_2 => new Rainbow(3f, 2.4f);
            public static Rainbow Rainbow_5sec => new Rainbow(5f, 1.8f);
            public static Rainbow Rainbow_5sec_2 => new Rainbow(5f, 3.5f);
            public static Rainbow Rainbow_10sec => new Rainbow(10f, 4.0f);
            public static Rainbow Rainbow_10sec_2 => new Rainbow(10f, 7.2f);
            public static Rainbow Pride => new Rainbow(3f, 0f, 1f)
            {
                isTrans = true,
            };
        }
        #endregion
        #region Raw Colors
        public struct Raw
        {
            public static Color32 Peach => new Color32(235, 160, 90, 255);
            public static Color32 FullBlue => new Color32(0, 0, 255, 255);
            public static Color32 FullGreen => new Color32(0, 255, 0, 255);
            public static Color32 FullRed => new Color32(255, 0, 0, 255);
            public static Color32 DeepBlue => new Color32(70, 70, 255, 255);
            public static Color32 DeepGreen => new Color32(70, 255, 70, 255);
            public static Color32 DeepRed => new Color32(255, 70, 70, 255);
            public static Color32 RedHealthBackground => new Color32(125, 0, 0, 255);
            public static Color32 GreenHealthForeground => new Color32(37, 255, 0, 255);
            public static Color32 PastelBlue => new Color32(140, 158, 255, 255);
            public static Color32 PastelGreen => new Color32(147, 255, 140, 255);
            public static Color32 PastelRed => new Color32(255, 140, 140, 255);
            public static Color32 PastelYellow => new Color32(250, 255, 140, 255);
            public static Color32 PastelPurple => new Color32(252, 140, 255, 255);
            public static Color32 PastelCyan => new Color32(140, 240, 255, 255);
            public static Color32 PastelOrange => new Color32(255, 216, 140, 255);
            public static Color32 White => new Color32(255, 255, 255, 255);
        }
        #endregion
        #region Gray
        public struct Gray
        {
            public static Color32 Gray1 => new Color32(25, 25, 25, 255);
            public static Color32 Gray2 => new Color32(51, 51, 51, 255);
            public static Color32 Gray3 => new Color32(76, 76, 76, 255);
            public static Color32 Gray4 => new Color32(102, 102, 102, 255);
            public static Color32 Gray5 => new Color32(127, 127, 127, 255);
            public static Color32 Gray6 => new Color32(152, 152, 152, 255);
            public static Color32 Gray7 => new Color32(178, 178, 178, 255);
            public static Color32 Gray8 => new Color32(203, 203, 203, 255);
            public static Color32 Gray9 => new Color32(229, 229, 229, 255);
            public static Color32 White => new Color32(255, 255, 255, 255);
        }
        #endregion
    }
    public static class ColorExtensions
    {
        public static Color32 Opacity(this Color c, byte alpha)
        {
            Color32 color = c;
            return new(color.r, color.g, color.b, alpha);
        }
        public static Color32 Opacity(this Color32 c, byte alpha)
        {
            Color32 color = c;
            return new(color.r, color.g, color.b, alpha);
        }
        public static Color32 LerpUnclamped(this Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.LerpUnclamped(a.r, b.r, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.LerpUnclamped(a.g, b.g, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.LerpUnclamped(a.b, b.b, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.LerpUnclamped(a.a, b.a, t)), 0, 255)
            );
        }
        public static Color32 SineAB(this Color32 a, Color32 b, float sineT)
        {
            float t = 0.5f + sineT.SineWave(Time.time, 0.5f);
            return a.LerpUnclamped(b, t);
        }
    }
}