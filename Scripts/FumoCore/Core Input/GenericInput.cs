using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace rinCore
{
    public static class GenericInputExtensions
    {
        public static bool IsPressed(this InputActionReference reference)
        {
            if (GeneralManager.IsPaused) return false;
            return GenericInput.GetTracker(reference)?.IsPressed ?? false;
        }
        public static bool JustPressed(this InputActionReference reference)
        {
            if (GeneralManager.IsPaused) return false;
            return GenericInput.GetTracker(reference)?.JustPressed ?? false;
        }
        public static bool PressedLongerThan(this InputActionReference reference, float seconds)
        {
            if (GeneralManager.IsPaused) return false;
            return GenericInput.GetTracker(reference)?.PressedLongerThan(seconds) ?? false;
        }
        public static bool ReleasedLongerThan(this InputActionReference reference, float seconds)
        {
            if (GeneralManager.IsPaused) return false;
            return GenericInput.GetTracker(reference)?.ReleasedLongerThan(seconds) ?? false;
        }
    }
    public static class InputActionRawExtensions
    {
        /// <summary>
        /// Reads the direct hardware state of all controls bound to this action.
        /// Bypasses the Action's internal state-machine/buffer.
        /// </summary>
        public static Vector2 ReadRawVector2(this InputActionReference reference)
        {
            if (reference == null || reference.action == null) return Vector2.zero;

            Vector2 combined = Vector2.zero;
            var controls = reference.action.controls;

            for (int i = 0; i < controls.Count; i++)
            {
                if (controls[i] is InputControl<Vector2> v2Control)
                {
                    combined += v2Control.ReadValue();
                }
                else if (controls[i] is InputControl<float> fControl)
                {
                    combined = reference.action.ReadValue<Vector2>();
                }
            }

            return Vector2.ClampMagnitude(combined, 1f);
        }

        /// <summary>
        /// Specifically for Mouse Delta which requires unprocessed state to feel 1:1.
        /// </summary>
        public static Vector2 ReadRawMouseDelta(this InputActionReference reference)
        {
            if (reference == null || reference.action == null) return Vector2.zero;
            if (Mouse.current != null) return Mouse.current.delta.ReadValue();
            return ReadRawVector2(reference);
        }

        /// <summary>
        /// Checks if any physical control bound to this action is actuated.
        /// </summary>
        public static bool IsPressedRaw(this InputActionReference reference, float threshold = 0.5f)
        {
            if (reference == null || reference.action == null) return false;

            var controls = reference.action.controls;
            for (int i = 0; i < controls.Count; i++)
            {
                if (controls[i].EvaluateMagnitude() >= threshold) return true;
            }
            return false;
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
        public static float FetchDeadzone()
        {
            if (PersistentJSON.TryLoad(out float value, "Stick Deadzone"))
                return UpdateDeadzone(value);
            else
                return UpdateDeadzone(0.4f);
        }

        [QFSW.QC.Command("-input-deadzone")]
        public static float UpdateDeadzone(float value)
        {
            stickDeadZone = Mathf.Clamp(value, 0.05f, 1f);
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
                if (JustPressed) PressStartTime = Time.unscaledTime;
                if (!currentlyPressed && IsPressed) ReleaseTime = Time.unscaledTime;
                if (!currentlyPressed) PressStartTime = -1f;
                IsPressed = currentlyPressed;
            }

            public bool PressedLongerThan(float duration) => IsPressed && PressStartTime >= 0f && (Time.unscaledTime - PressStartTime) >= duration;
            public bool ReleasedLongerThan(float duration) => !IsPressed && (ReleaseTime < 0f || (Time.unscaledTime - ReleaseTime) >= duration);
        }

        private readonly Dictionary<InputActionReference, ButtonStateTracker> trackers = new();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            if (moveInput != null) moveInput.action.Enable();
            if (lookInput != null) lookInput.action.Enable();
        }

        private void Update()
        {
            cachedMove = moveInput.ReadRawVector2();
            cachedLook = lookInput.ReadRawVector2();

            foreach (var kvp in trackers)
            {
                if (kvp.Key == null) continue;
                kvp.Value.Update(kvp.Key.IsPressedRaw());
            }
        }

        internal static ButtonStateTracker GetTracker(InputActionReference reference)
        {
            if (instance == null || reference == null) return null;
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
