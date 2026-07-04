using UnityEngine;

namespace rinCore
{
    public interface IVelocity
    {
        public static IVelocity Player;
        public static Vector2 PlayerPortraitXY => Player != null ? Player.PortraitRotation : Vector2.zero;
        public Vector3 CurrentVelocity { get; }
        public Vector2 PortraitRotation { get; }
    }
}
