using UnityEngine;

namespace FumoCore.Tools
{
    public static class DoublePlayerPrefsExtensions
    {
        public static void StoreKey(this double value, string key)
        {
            PlayerPrefs.SetString(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
        public static double FetchKey(this double _, string key)
        {
            if (!PlayerPrefs.HasKey(key))
                return 0;

            string stringValue = PlayerPrefs.GetString(key);
            if (double.TryParse(stringValue, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            else
            {
                Debug.LogWarning($"Failed to parse double from PlayerPrefs key '{key}': {stringValue}");
                return 0;
            }
        }
    }
}
