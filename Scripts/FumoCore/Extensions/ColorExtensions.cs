using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RinCore
{
    public struct ColorHelper
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
        #region Gray
        public static Color32 Gray1 => new Color32(25, 25, 25, 255);
        public static Color32 Gray2 => new Color32(51, 51, 51, 255);
        public static Color32 Gray3 => new Color32(76, 76, 76, 255);
        public static Color32 Gray4 => new Color32(102, 102,102, 255);
        public static Color32 Gray5 => new Color32(127, 127, 127, 255);
        public static Color32 Gray6 => new Color32(152, 152, 152, 255);
        public static Color32 Gray7 => new Color32(178, 178, 178, 255);
        public static Color32 Gray8 => new Color32(203, 203, 203, 255);
        public static Color32 Gray9 => new Color32(229, 229, 229, 255);
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
    }
}