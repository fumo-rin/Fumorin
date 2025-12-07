using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [DefaultExecutionOrder(-100)]
    internal class GenericInput : MonoBehaviour
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
        [SerializeField] InputActionReference moveInput, lookInput;
        static Vector2 cachedMove, cachedLook;
        public static Vector2 Look => instance == null ? Vector2.zero : cachedLook;
        public static Vector2 Move => instance == null ? Vector2.zero : cachedMove;
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
