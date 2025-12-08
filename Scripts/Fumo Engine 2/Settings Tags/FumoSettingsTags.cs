using System.Collections.Generic;
using UnityEngine;

namespace RinCore
{
    public static class FumoSettingsTags
    {
        #region Keys
        public struct Keys
        {
            public static string PlayerShotVisibility => "PLAYER_SHOT_VISIBILITY_REDUCTION";
        }
        #endregion
        [System.Serializable]
        public struct SettingTagBool
        {
            public string tagName;
            public bool active;

            public SettingTagBool(string tag, bool active)
            {
                this.tagName = tag;
                this.active = active;
            }
        }
        static readonly string saveKey = "SettingsTags";
        static List<SettingTagBool> boolSettings = new();
        static Dictionary<string, bool> boolSettingsCache = new();
        static bool initialized = false;
        [RinCore.Initialize(-99999)]
        private static void ResetFetch()
        {
            initialized = false;
            RefetchSettings(out _);
        }
        public static void RefetchSettings(out List<SettingTagBool> result)
        {
            if (initialized)
            {
                result = boolSettings;
                return;
            }
            boolSettings.Clear();
            boolSettingsCache.Clear();
            if (!PersistentJSON.TryLoad(out List<SettingTagBool> settings, saveKey))
            {
                result = boolSettings;
                initialized = true;
                return;
            }
            boolSettings = settings;
            foreach (var item in boolSettings)
            {
                boolSettingsCache[item.tagName] = item.active;
            }
            initialized = true;
            result = boolSettings;
        }
        public static bool HasBoolTag(string key)
        {
            if (!initialized)
                RefetchSettings(out _);
            return boolSettingsCache.TryGetValue(key, out bool active) && active;
        }
        public static void SetBoolTag(SettingTagBool tag)
        {
            if (!initialized)
                RefetchSettings(out _);
            boolSettingsCache[tag.tagName] = tag.active;
            int idx = boolSettings.FindIndex(x => x.tagName == tag.tagName);
            if (idx >= 0)
                boolSettings[idx] = tag;
            else
                boolSettings.Add(tag);
            StoreSettings();
        }
        public static void StoreSettings()
        {
            PersistentJSON.TrySave(boolSettings, saveKey);
        }
    }
}
