using RinCore;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RinCore
{
    public class RebindButton : MonoBehaviour
    {
        [field: SerializeField] public Button b1 { get; private set; }
        [field: SerializeField] public Button b2 { get; private set; }
        [SerializeField] TMP_Text bindingNameText;

        [SerializeField] InputActionReference bindAction;
        RebindHandler handler;

        static InputActionRebindingExtensions.RebindingOperation rebindingOperation;
        static RebindButton currentRebind;
        static int lastBindingIndex;

        const int MainBindingIndex = 0;
        const int AltBindingIndex = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ReinitializeStuff()
        {
            currentRebind = null;
            lastBindingIndex = 0;
            if (rebindingOperation != null)
            {
                rebindingOperation.Dispose();
            }
        }
        public void AssignRebindHandler(RebindHandler handler, InputActionReference inputAction)
        {
            this.handler = handler;
            bindAction = inputAction;
            bindingNameText.text = inputAction.action.name;
        }

        private void Start()
        {
            LoadBinds();
            b1.onClick.AddListener(() => StartRebinding(MainBindingIndex));
            b2.onClick.AddListener(() => StartRebinding(AltBindingIndex));

            FetchBindingText(MainBindingIndex);
            FetchBindingText(AltBindingIndex);
        }

        private void OnDestroy()
        {
            b1.onClick.RemoveAllListeners();
            b2.onClick.RemoveAllListeners();
            EndCurrentBind();
        }
        public static void EndCurrentBind()
        {
            if (currentRebind != null)
                currentRebind.RebindComplete(lastBindingIndex);
            currentRebind = null;
        }
        private void StartRebinding(int targetBindingIndex)
        {
            EndCurrentBind();
            lastBindingIndex = targetBindingIndex;
            currentRebind = this;

            bindAction.action.Disable();

            TMP_Text buttonText = (targetBindingIndex == MainBindingIndex) ?
                b1.GetComponentInChildren<TMP_Text>() :
                b2.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
                buttonText.text = "Waiting for input...";

            rebindingOperation = bindAction.action.PerformInteractiveRebinding(targetBindingIndex)
                .WithControlsExcluding("Mouse")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op => RebindComplete(targetBindingIndex))
                .Start();
        }

        public void FetchBindingText(int index)
        {
            IEnumerator CO_Stall()
            {
                bindAction.action.Enable();
                yield return null;

                if (bindAction?.action == null || index >= bindAction.action.bindings.Count)
                {
                    Debug.LogWarning("Invalid action or index.");
                    yield break;
                }

                string readable = InputControlPath.ToHumanReadableString(
                    bindAction.action.bindings[index].effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);

                TMP_Text buttonText = (index == MainBindingIndex) ?
                    b1.GetComponentInChildren<TMP_Text>() :
                    b2.GetComponentInChildren<TMP_Text>();

                if (buttonText != null)
                    buttonText.text = readable;
            }

            handler.StartCoroutine(CO_Stall());
        }

        private void RebindComplete(int index)
        {
            rebindingOperation?.Dispose();
            bindAction.action.Enable();
            SaveBinds();
            FetchBindingText(index);
            currentRebind = null;
        }

        public void SaveBinds()
        {
            string rebinds = bindAction.action.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(bindAction.action.name, rebinds);
        }

        public void LoadBinds()
        {
            string rebinds = PlayerPrefs.GetString(bindAction.action.name, string.Empty);
            if (!string.IsNullOrEmpty(rebinds))
                bindAction.action.LoadBindingOverridesFromJson(rebinds);
        }
    }
}