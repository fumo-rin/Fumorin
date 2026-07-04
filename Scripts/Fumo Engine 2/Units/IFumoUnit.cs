using UnityEngine;

namespace rinCore
{
    public interface IFumoUnit
    {
        public static IFumoUnit Player;
        public struct playerPosition
        {
            public bool HasPlayer;
            public Vector3 Position;
        }
        public bool IsAlive { get; set; }
        public Vector3 CurrentPosition { get; }
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
    }
}
