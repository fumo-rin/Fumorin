using UnityEngine;
using UnityEngine.InputSystem;

namespace RinCore
{
    public static partial class RinHelper
    {
        public static Vector2 MousePosition => GetMousePosition();
        private static Vector2 GetMousePosition()
        {
            if (Camera.main != null)
            {
                Vector2 mouseXY = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                return mouseXY;
            }
            Debug.LogError("Failed to find mouse position, See MousePosition.cs");
            return Vector2.zero;
        }
    }
}
