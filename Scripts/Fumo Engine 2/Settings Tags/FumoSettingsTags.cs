using System;
using System.Collections.Generic;
using UnityEngine;

namespace RinCore
{
    public static class FumoSettingsTags
    {
        #region Keys / Demo Enums
        public enum KeysShmup
        {
            PlayerShotVisibilityReduction,
            PlayerAutoFireWhenFocused
        }
        #endregion

        #region Commands
        private static class Commands
        {
            [QFSW.QC.Command("-settings-tags-print")]
            private static void PrintTags()
            {
                foreach (var item in EnumHelper.ForeachReadableNames<KeysShmup>())
                {
                    Debug.Log(item);
                }
            }
            [QFSW.QC.Command("-settings-tag-add")]
            private static void AddTagCommand(string key, bool state)
            {
                SetBoolTag(new SettingTagBool(key, state));
            }
            [QFSW.QC.Command("-settings-tag-remove")]
            private static void RemoveTagCommand(string key)
            {
                RemoveBoolTag(key);
            }
        }
        #endregion

        [System.Serializable]
        public struct SettingTagBool
        {
            public string tagName;
            public bool active;

            public SettingTagBool(string tag, bool active)
            {
                tagName = tag;
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

        #region Enum-based API
        public static void SetBoolTag<T>(T tag, bool state) where T : Enum
        {
            string key = tag.ReadableFullString();
            SetBoolTag(new SettingTagBool(key, state));
        }

        public static bool HasBoolTag<T>(T tag) where T : Enum
        {
            string key = tag.ReadableFullString();
            return HasBoolTag(key);
        }

        public static void RemoveBoolTag<T>(T tag) where T : Enum
        {
            string key = tag.ReadableFullString();
            RemoveBoolTag(key);
        }
        #endregion

        #region String-based core API
        public static bool HasBoolTag(string key)
        {
            if (!initialized)
                RefetchSettings(out _);

            key = string.Intern(key);
            return boolSettingsCache.TryGetValue(key, out bool active) && active;
        }

        public static void SetBoolTag(SettingTagBool tag)
        {
            if (!initialized)
                RefetchSettings(out _);

            tag.tagName = string.Intern(tag.tagName);
            boolSettingsCache[tag.tagName] = tag.active;

            int idx = boolSettings.FindIndex(x => x.tagName == tag.tagName);
            if (idx >= 0)
                boolSettings[idx] = tag;
            else
                boolSettings.Add(tag);

            StoreSettings();
        }

        public static void RemoveBoolTag(string tag)
        {
            if (!initialized)
                RefetchSettings(out _);

            tag = string.Intern(tag);
            boolSettingsCache.Remove(tag);
            boolSettings.RemoveAll(x => x.tagName == tag);

            StoreSettings();
        }
        #endregion

        public static void StoreSettings()
        {
            PersistentJSON.TrySave(boolSettings, saveKey);
        }
    }
}