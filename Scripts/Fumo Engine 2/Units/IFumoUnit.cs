using UnityEngine;

namespace rinCore
{
    public interface IFumoUnit
    {
        public GameObject unitGameObject { get; }
        public Rigidbody UnitRB { get; }
        public static IFumoUnit Player;
        public bool IsPlayer => Player == this;
        public struct playerPosition
        {
            public bool HasPlayer;
            public Vector3 Position;
        }
        public bool IsAlive { get; set; }
        public Vector3 CurrentPosition { get; }
        public Vector3 Center { get; }
        public static playerPosition PlayerPosition
        {
            get
            {
                playerPosition result = new();
                result.HasPlayer = Player != null && Player.IsAlive;
                result.Position = Player.CurrentPosition;
                return result;
            }
        }
        public void SnapTo(Vector3 v, Vector3? offset = null);
        public void SnapTo(Transform t);
    }
}
