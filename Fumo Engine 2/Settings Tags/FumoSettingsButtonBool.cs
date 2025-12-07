using UnityEngine;
using UnityEngine.UI;

namespace RinCore
{
    [RequireComponent(typeof(Toggle))]
    public class FumoSettingsButtonBool : MonoBehaviour
    {
        Toggle t;
        [SerializeField] string settingKey = "Dummy Setting";
        private void Awake()
        {
            t = GetComponent<Toggle>();

            bool currentValue = FumoSettingsTags.HasBoolTag(settingKey);
            t.isOn = currentValue;

            t.onValueChanged.RemoveAllListeners();
            t.onValueChanged.AddListener(PressFlipSetting);
        }
        void PressFlipSetting(bool newValue)
        {
            bool currentValue = FumoSettingsTags.HasBoolTag(settingKey);
            FumoSettingsTags.SetBoolTag(new FumoSettingsTags.SettingTagBool(settingKey, !currentValue));
        }
    }
}
