using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;

namespace rinCore
{
    [DefaultExecutionOrder(1)]
    public class RebindHandler : MonoBehaviour
    {
        static RebindHandler instance;

        [SerializeField] RebindButton buttonPrefab;
        [SerializeField] RebindButtonVec2 vec2ButtonPrefab;
        [SerializeField] Button resetDefaultsButton;
        [SerializeField] Transform buttonsContainer;
        [SerializeField] GameObject toggleAnchor;
        [SerializeField] GameObject[] extraToggles;
        [SerializeField] List<InputActionReference> setableBinds = new();
        [SerializeField] List<InputActionReference> vec2Binds = new();
        [SerializeField] bool startOpen = false;

        public static InputActionRebindingExtensions.RebindingOperation rebindingOperation;

        readonly HashSet<RebindButton> buttons = new();
        readonly HashSet<RebindButtonVec2> vec2Buttons = new();

        public static bool IsVisible => instance != null && instance.toggleAnchor.activeInHierarchy;

        void Awake()
        {
            instance = this;

            SetUIVisibility(startOpen);

            if (buttonPrefab != null)
                buttonPrefab.gameObject.SetActive(false);

            if (vec2ButtonPrefab != null)
                vec2ButtonPrefab.gameObject.SetActive(false);
        }

        void Start()
        {
            foreach (var item in vec2Binds)
            {
                item.asset.Enable();

                var spawned = Instantiate(vec2ButtonPrefab, buttonsContainer);

                spawned.gameObject.SetActive(true);
                spawned.Assign(item);

                vec2Buttons.Add(spawned);
            }
            foreach (var item in setableBinds)
            { 
                item.asset.Enable();

                var spawned = Instantiate(buttonPrefab, buttonsContainer);

                spawned.gameObject.SetActive(true);
                spawned.AssignRebindHandler(this, item);

                buttons.Add(spawned);
            }

            if (resetDefaultsButton != null)
            {
                resetDefaultsButton.BindSingleAction(ResetToDefaults);
            }
            RefetchAllKeybinds();
        }

        void RefetchAllKeybinds()
        {
            foreach (var item in buttons)
            {
                item.LoadBinds();
                item.FetchBindingText(0);
                item.FetchBindingText(1);
            }

            foreach (var item in vec2Buttons)
            {
                item.LoadBinds();
                item.RefreshUI();
            }
        }

        public static void SetUIVisibility(bool state)
        {
            if (instance == null)
                return;

            if (instance.toggleAnchor) instance.toggleAnchor.SetActive(state);

            foreach (var item in instance.extraToggles)
                item?.SetActive(state);
        }

        public static void ResetToDefaults()
        {
            if (instance == null)
                return;

            RebindButton.EndCurrentBind();

            rebindingOperation?.Dispose();

            foreach (var item in instance.setableBinds)
                item.action.RemoveAllBindingOverrides();

            foreach (var item in instance.vec2Binds)
                item.action.RemoveAllBindingOverrides();

            foreach (var item in instance.buttons)
                item.SaveBinds();

            foreach (var item in instance.vec2Buttons)
                item.SaveBinds();

            instance.RefetchAllKeybinds();
        }

        public static bool TryGetReadableBindings(InputActionReference actionRef, out string result)
        {
            result = "[Invalid Action]";

            if (actionRef == null || actionRef.action == null)
                return false;

            var bindings = actionRef.action.bindings;

            if (bindings.Count == 0)
            {
                result = "[Unbound]";
                return false;
            }

            if (bindings[0].isComposite)
            {
                string up = Read(bindings, 1);
                string down = Read(bindings, 2);
                string left = Read(bindings, 3);
                string right = Read(bindings, 4);

                result = $"{up}/{left}/{down}/{right}";
                return true;
            }

            List<string> readable = new();

            for (int i = 0; i < Mathf.Min(2, bindings.Count); i++)
            {
                string r = Read(bindings, i);

                if (!string.IsNullOrEmpty(r))
                    readable.Add(r);
            }

            if (readable.Count == 0)
            {
                result = "[Unbound]";
                return false;
            }

            result = string.Join(" or ", readable);

            return true;
        }

        static string Read(IReadOnlyList<InputBinding> bindings, int index)
        {
            if (index >= bindings.Count)
                return "";

            string path = bindings[index].effectivePath;

            if (string.IsNullOrEmpty(path))
                return "";

            return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }
}