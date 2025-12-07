using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using FumoCore.Input;
using System.Collections.Generic;
using System.IO;

namespace Fumorin
{
    [DefaultExecutionOrder(1)]
    public class RebindHandler : MonoBehaviour
    {
        static RebindHandler instance;

        [SerializeField] RebindButton buttonPrefab;
        [SerializeField] Transform buttonsContainer;
        [SerializeField] GameObject toggleAnchor;
        [SerializeField] GameObject[] extraToggles;
        [SerializeField] List<InputActionReference> setableBinds = new();
        [SerializeField] bool startOpen = false;

        HashSet<RebindButton> CreatedButtons = new();
        public static bool IsVisible => instance == null ? false : instance.toggleAnchor.activeInHierarchy;
        public static bool TryGetReadableBindings(InputActionReference actionRef, out string result)
        {
            result = "[Invalid Action]";
            if (actionRef == null || actionRef.action == null)
                return false;
            var action = actionRef.action;
            action.Enable();
            List<string> readable = new();
            for (int i = 0; i < Mathf.Min(2, action.bindings.Count); i++)
            {
                string path = action.bindings[i].effectivePath;
                if (string.IsNullOrEmpty(path))
                    continue;
                string humanReadable = InputControlPath.ToHumanReadableString(
                    path,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
                readable.Add(humanReadable);
            }
            if (readable.Count == 0)
            {
                result = "[Unbound]";
                return false;
            }
            result = string.Join(" or ", readable);
            return true;
        }
        private void Awake()
        {
            instance = this;
            CreatedButtons.Clear();
            SetUIVisibility(startOpen);
        }

        private void Start()
        {
            int iteration = 0;
            foreach (var item in setableBinds)
            {
                item.asset.Enable();
                RebindButton spawned = Instantiate(buttonPrefab, buttonsContainer);
                NavigationElement e = null;
                if (iteration == 0)
                {
                    e = spawned.b1.gameObject.AddComponent<NavigationElement>();
                    e.weight = 15;
                    e.IsLayerDefaultSelection = true;
                }
                CreatedButtons.Add(spawned);
                spawned.AssignRebindHandler(this, item);
            }
            RefetchAllKeybinds();
        }
        private void RefetchAllKeybinds()
        {
            foreach (var item in CreatedButtons)
            {
                item.LoadBinds();
                item.FetchBindingText(0);
                item.FetchBindingText(1);
            }
        }
        public static void SetUIVisibility(bool state)
        {
            if (instance == null)
            {
                return;
            }
            if (instance.toggleAnchor != null) instance.toggleAnchor.SetActive(state);
            foreach (var item in instance.extraToggles)
            {
                item.SetActive(state);
            }
        }
        public static void ResetToDefaults()
        {
            if (instance == null)
                return;
            RebindButton.EndCurrentBind();
            foreach (var actionRef in instance.setableBinds)
            {
                actionRef.action.RemoveAllBindingOverrides();
            }
            foreach (var rebind in instance.CreatedButtons)
            {
                rebind.SaveBinds();
            }
            instance.RefetchAllKeybinds();
        }
    }
}