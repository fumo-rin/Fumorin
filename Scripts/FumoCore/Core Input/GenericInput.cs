using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RinCore
{
    public static class GenericInputExtensions
    {
        public static bool IsPressed(this InputActionReference reference)
        {
            return GenericInput.GetTracker(reference)?.IsPressed ?? false;
        }
        public static bool JustPressed(this InputActionReference reference)
        {
            return GenericInput.GetTracker(reference)?.JustPressed ?? false;
        }
        public static bool PressedLongerThan(this InputActionReference reference, float seconds)
        {
            return GenericInput.GetTracker(reference)?.PressedLongerThan(seconds) ?? false;
        }
        public static bool ReleasedLongerThan(this InputActionReference reference, float seconds)
        {
            return GenericInput.GetTracker(reference)?.ReleasedLongerThan(seconds) ?? false;
        }
    }
    #region Sticks & Deadzone
    public partial class GenericInput
    {
        static float stickDeadZone = 0.4f;
        [SerializeField] InputActionReference moveInput, lookInput;
        static Vector2 cachedMove, cachedLook;
        public static Vector2 Look => instance == null ? Vector2.zero : cachedLook.magnitude >= stickDeadZone.Clamp(0.05f, 0.95f) ? cachedLook : Vector2.zero;
        public static Vector2 Move => instance == null ? Vector2.zero : cachedMove.magnitude >= stickDeadZone.Clamp(0.05f, 0.95f) ? cachedMove : Vector2.zero;
        [Initialize(999)]
        public static float FetchDeadzone()
        {
            if (PersistentJSON.TryLoad(out float value, "Stick Deadzone"))
            {
                return UpdateDeadzone(value);
            }
            else
            {
                return UpdateDeadzone(0.4f);
            }
        }
        [QFSW.QC.Command("-input-deadzone")]
        public static float UpdateDeadzone(float value)
        {
            stickDeadZone = value.Clamp(0.05f, 1f);
            PersistentJSON.TrySave(stickDeadZone, "Stick Deadzone");
            Debug.Log("Updated stick deadzone :" + value.ToString("F2"));
            return stickDeadZone;
        }
    }
    #endregion
    [DefaultExecutionOrder(-100)]
    public partial class GenericInput : MonoBehaviour
    {
        private static GenericInput instance;
        internal class ButtonStateTracker
        {
            public bool IsPressed { get; private set; }
            public bool JustPressed { get; private set; }
            public float PressStartTime { get; private set; } = -1f;
            public float ReleaseTime { get; private set; } = -1f;

            public void Update(bool currentlyPressed)
            {
                JustPressed = currentlyPressed && !IsPressed;

                if (JustPressed)
                    PressStartTime = Time.unscaledTime;

                if (!currentlyPressed && IsPressed)
                    ReleaseTime = Time.unscaledTime;

                if (!currentlyPressed)
                    PressStartTime = -1f;

                IsPressed = currentlyPressed;
            }

            public bool PressedLongerThan(float duration)
            {
                return IsPressed && PressStartTime >= 0f && (Time.unscaledTime - PressStartTime) >= duration;
            }

            public bool ReleasedLongerThan(float duration)
            {
                return !IsPressed && (ReleaseTime < 0f || (Time.unscaledTime - ReleaseTime) >= duration);
            }
        }
        private readonly Dictionary<InputActionReference, ButtonStateTracker> trackers = new();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            moveInput.action.Enable();
            lookInput.action.Disable();
            instance = this;
        }
        private void Update()
        {
            foreach (var kvp in trackers)
            {
                InputActionReference reference = kvp.Key;
                if (reference == null || reference.action == null)
                    continue;

                bool pressed = reference.action.IsPressed();
                kvp.Value.Update(pressed);
            }
            cachedLook = lookInput.action.ReadValue<Vector2>();
            cachedMove = moveInput.action.ReadValue<Vector2>();
        }

        internal static ButtonStateTracker GetTracker(InputActionReference reference)
        {
            if (instance == null)
            {
                return null;
            }

            if (reference == null)
                return null;

            if (!instance.trackers.TryGetValue(reference, out var tracker))
            {
                tracker = new ButtonStateTracker();
                instance.trackers[reference] = tracker;
                reference.action.Enable();
            }

            return tracker;
        }
    }
}
