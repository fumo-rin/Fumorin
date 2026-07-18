using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [DefaultExecutionOrder(-123)]
    public static class InputActionRawExtensions
    {
        /// <summary>
        /// Reads direct hardware values for physical movement states. 
        /// </summary>
        public static Vector2 ReadRawVector2(this InputActionReference reference, bool clamp)
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

            if (clamp)
            {
                return Vector2.ClampMagnitude(combined, 1f);
            }
            return combined;
        }

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
        static float cachedDeadZone;
        static float stickDeadZone
        {
            get => cachedDeadZone;
            set
            {
                cachedDeadZone = value;
                Debug.Log("Set Deadzone: " + cachedDeadZone);
            }
        }

        [SerializeField] InputActionReference moveInput, lookInput;
        static Vector2 cachedMove, cachedLook;

        // This is your pure raw look vector. No processing, no normalization filters.
        public static Vector2 Look => instance != null ? cachedLook : Vector2.zero;

        public static Vector2 Move
        {
            get
            {
                if (instance == null) return Vector2.zero;
                ProcessWithDeadzone(cachedMove, out var result);
                return result;
            }
        }

        public static bool IsLookUsingMouse
        {
            get
            {
                if (instance == null || instance.lookInput == null) return false;
                var active = instance.lookInput.action?.activeControl;
                return active != null && active.device is Pointer;
            }
        }

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
            stickDeadZone = Mathf.Clamp(value, 0.1f, 0.8f);
            PersistentJSON.TrySave(stickDeadZone, "Stick Deadzone");
            Debug.Log("Updated stick deadzone :" + value.ToString("F2"));
            return stickDeadZone;
        }

        public static bool ProcessWithDeadzone(in Vector2 input, out Vector2 result)
        {
            result.x = Mathf.Abs(input.x) >= stickDeadZone ? input.x : 0f;
            result.y = Mathf.Abs(input.y) >= stickDeadZone ? input.y : 0f;
            return result != Vector2.zero;
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

            if (moveInput != null && moveInput.action != null) moveInput.action.Enable();
            if (lookInput != null && lookInput.action != null) lookInput.action.Enable();

            transform.SetParent(null);
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            UpdateDeadzone(FetchDeadzone());
        }

        private void Update()
        {
            cachedMove = moveInput != null ? moveInput.ReadRawVector2(true) : Vector2.zero;

            if (lookInput != null && lookInput.action != null)
            {
                cachedLook = lookInput.ReadRawVector2(false);
            }
            else
            {
                cachedLook = Vector2.zero;
            }

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
                if (reference.action != null) reference.action.Enable();
            }
            return tracker;
        }
    }
}