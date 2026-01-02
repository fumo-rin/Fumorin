using System.Collections.Generic;
using UnityEngine;
using TMPro;
using RinCore;

namespace rinCore
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class ResetWindow : MonoBehaviour
    {
        private static readonly string persistenceKey = "SelectedResolutionIndex";
        [System.Serializable]
        public struct ResolutionOption
        {
            public int width;
            public int height;
        }

        [SerializeField]
        private List<ResolutionOption> resolutions = new List<ResolutionOption>();

        private TMP_Dropdown dropdown;

        private void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
            if (GeneralManager.IsWebGL)
            {
                dropdown.interactable = false;
            }
        }

        private void Start()
        {
            PopulateDropdown();
            RestoreSelection();
            dropdown.onValueChanged.AddListener(OnResolutionSelected);
        }

        private void OnDestroy()
        {
            dropdown.onValueChanged.RemoveListener(OnResolutionSelected);
        }

        private void PopulateDropdown()
        {
            dropdown.ClearOptions();

            var options = new List<string>();
            foreach (var res in resolutions)
            {
                options.Add($"{res.width}x{res.height}");
            }

            dropdown.AddOptions(options);
        }

        private void RestoreSelection()
        {
            if (PersistentJSON.TryLoad(out int savedIndex, persistenceKey))
            {
                if (savedIndex >= 0 && savedIndex < resolutions.Count)
                {
                    dropdown.SetValueWithoutNotify(savedIndex);
                    return;
                }
            }
            ApplyResolution(dropdown.value);
        }

        private void OnResolutionSelected(int index)
        {
            if (index < 0 || index >= resolutions.Count)
                return;

            PersistentJSON.TrySave(index, persistenceKey);
            ApplyResolution(index);
        }

        private void ApplyResolution(int index)
        {
            var res = resolutions[index];
            var refreshRate = Screen.currentResolution.refreshRateRatio;

            Screen.SetResolution(
                res.width,
                res.height,
                FullScreenMode.Windowed,
                refreshRate
            );
        }
    }
}
