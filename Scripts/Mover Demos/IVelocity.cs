using UnityEngine;

namespace rinCore
{
    public interface IVelocity
    {
        public static IVelocity Player;
        public static Vector2 PlayerPortraitXY => Player != null ? Player.PortraitRotation : Vector2.zero;
        public static Vector2 PlayerRelativePlanarMovementXY => Player != null ? Player.RelativePlanarMovementXY : Vector2.zero;
        public Vector3 CurrentPosition { get; }
        public Vector3 CurrentVelocity { get; }
        public Vector2 PortraitRotation { get; }
        public Vector2 RelativePlanarMovementXY { get; }
    }
}
