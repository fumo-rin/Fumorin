using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace rinCore
{
    internal class RebindButtonVec2 : MonoBehaviour
    {
        [SerializeField] Button upButton;
        [SerializeField] Button downButton;
        [SerializeField] Button leftButton;
        [SerializeField] Button rightButton;

        [SerializeField] TMP_Text upText;
        [SerializeField] TMP_Text downText;
        [SerializeField] TMP_Text leftText;
        [SerializeField] TMP_Text rightText;

        InputActionReference bindAction;
        [SerializeField] TMP_Text bindingNameText;

        int UpIndex => FindCompositePart("up");
        int DownIndex => FindCompositePart("down");
        int LeftIndex => FindCompositePart("left");
        int RightIndex => FindCompositePart("right");

        void Start()
        {
            LoadBinds();

            RefreshUI();

            upButton.onClick.AddListener(() => StartRebinding(UpIndex, upText));
            downButton.onClick.AddListener(() => StartRebinding(DownIndex, downText));
            leftButton.onClick.AddListener(() => StartRebinding(LeftIndex, leftText));
            rightButton.onClick.AddListener(() => StartRebinding(RightIndex, rightText));
        }

        void OnDestroy()
        {
            upButton.onClick.RemoveAllListeners();
            downButton.onClick.RemoveAllListeners();
            leftButton.onClick.RemoveAllListeners();
            rightButton.onClick.RemoveAllListeners();

            RebindHandler.rebindingOperation?.Dispose();
        }

        int FindCompositePart(string name)
        {
            var bindings = bindAction.action.bindings;

            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].isPartOfComposite &&
                    bindings[i].name.ToLower() == name)
                {
                    return i;
                }
            }

            return -1;
        }

        void StartRebinding(int bindingIndex, TMP_Text targetText)
        {
            if (bindingIndex < 0)
                return;

            RebindHandler.rebindingOperation?.Dispose();

            bindAction.action.Disable();

            targetText.text = "Waiting...";

            RebindHandler.rebindingOperation = bindAction.action
                .PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .OnComplete(op =>
                {
                    op.Dispose();

                    bindAction.action.Enable();

                    SaveBinds();

                    RefreshUI();
                })
                .Start();
        }

        public void RefreshUI()
        {
            upText.text = GetBindingName(UpIndex);
            downText.text = GetBindingName(DownIndex);
            leftText.text = GetBindingName(LeftIndex);
            rightText.text = GetBindingName(RightIndex);
        }

        public void Assign(InputActionReference action)
        {
            bindAction = action;
            bindingNameText.text = action.action.name;
        }

        string GetBindingName(int index)
        {
            if (index < 0 || index >= bindAction.action.bindings.Count)
                return "[NONE]";

            string path = bindAction.action.bindings[index].effectivePath;

            if (string.IsNullOrEmpty(path))
                return "[NONE]";

            return InputControlPath.ToHumanReadableString(
                path,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        public void SaveBinds()
        {
            string rebinds = bindAction.action.SaveBindingOverridesAsJson();

            PlayerPrefs.SetString(bindAction.action.id.ToString(), rebinds);
        }

        public void LoadBinds()
        {
            string rebinds = PlayerPrefs.GetString(
                bindAction.action.id.ToString(),
                string.Empty);

            if (!string.IsNullOrEmpty(rebinds))
                bindAction.action.LoadBindingOverridesFromJson(rebinds);
        }
    }
}