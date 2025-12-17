using UnityEngine;
using UnityEngine.InputSystem;

namespace RinCore
{
    [DefaultExecutionOrder(-100)]
    public class ShmupInput : MonoBehaviour
    {
        [System.Serializable]
        private class Map
        {
            public InputActionReference shootAction;
            public InputActionReference focusAction;
            public InputActionReference bombAction;
            public InputActionReference skipDialogueAction;
            public InputActionReference reloadPracticeWarp;
            public InputActionReference chargeAction;
        }
        private void Awake()
        {
            instance = this;
        }
        [SerializeField] private Map inputMap;
        private static ShmupInput instance;
        public static Vector2 Move
        {
            get
            {
                return GenericInput.Move.normalized;
            }
        }
        public static bool Shoot => instance == null ? false : instance.inputMap.shootAction.IsPressed();
        public static bool ShootJustPressed => instance?.inputMap.shootAction.JustPressed() ?? false;
        public static bool ShootPressedLongerThan(float seconds) => instance?.inputMap.shootAction.PressedLongerThan(seconds) ?? false;
        public static bool ShootReleasedLongerThan(float seconds) => instance?.inputMap.shootAction.ReleasedLongerThan(seconds) ?? false;
        public static bool Focus => instance?.inputMap.focusAction.IsPressed() ?? false;
        public static bool FocusJustPressed => instance?.inputMap.focusAction.JustPressed() ?? false;
        public static bool FocusPressedLongerThan(float seconds) => instance?.inputMap.focusAction.PressedLongerThan(seconds) ?? false;
        public static bool FocusReleasedLongerThan(float seconds) => instance?.inputMap.focusAction.ReleasedLongerThan(seconds) ?? false;
        public static bool Bomb => instance?.inputMap.bombAction.IsPressed() ?? false;
        public static bool BombJustPressed => instance?.inputMap.bombAction.JustPressed() ?? false;
        public static bool BombPressedLongerThan(float seconds) => instance?.inputMap.bombAction.PressedLongerThan(seconds) ?? false;
        public static bool BombReleasedLongerThan(float seconds) => instance?.inputMap.bombAction.ReleasedLongerThan(seconds) ?? false;
        public static bool SkipDialogue => instance?.inputMap.skipDialogueAction.IsPressed() ?? false;
        public static bool SkipDialogueJustPressed => instance?.inputMap.skipDialogueAction.JustPressed() ?? false;
        public static bool SkipDialoguePressedLongerThan(float seconds) => instance?.inputMap.skipDialogueAction.PressedLongerThan(seconds) ?? false;
        public static bool SkipDialogueReleasedLongerThan(float seconds) => instance?.inputMap.skipDialogueAction.ReleasedLongerThan(seconds) ?? false;
        public static bool ReloadPractice => instance?.inputMap.reloadPracticeWarp.IsPressed() ?? false;
        public static bool ReloadPracticeJustPressed => instance?.inputMap.reloadPracticeWarp.JustPressed() ?? false;
        public static bool ReloadPracticePressedLongerThan(float seconds) => instance?.inputMap.reloadPracticeWarp.PressedLongerThan(seconds) ?? false;
        public static bool ReloadPracticeReleasedLongerThan(float seconds) => instance?.inputMap.reloadPracticeWarp.ReleasedLongerThan(seconds) ?? false;
        public static bool Charging => instance?.inputMap.chargeAction.IsPressed() ?? false;
        public static bool ChargeJustPressed => instance?.inputMap.chargeAction.JustPressed() ?? false;
        public static bool ChargePressedLongerThan(float seconds) => instance?.inputMap.chargeAction.PressedLongerThan(seconds) ?? false;
        public static bool ChargeReleasedLongerThan(float seconds) => instance?.inputMap.chargeAction.ReleasedLongerThan(seconds) ?? false;
    }
}
