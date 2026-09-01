using UnityEngine;

namespace rinCore
{
    public enum Cardinal
    {
        Right = 0, Up = 1, Left = 2, Down = 3
    }

    public static class CardinalExt
    {
        public static Cardinal Rotate(this Cardinal c, int count)
        {
            return (Cardinal)(((int)c + count) % 4);
        }
        public static Cardinal Flip(this Cardinal c)
        {
            return Rotate(c, 2);
        }

        public static Vector2Int Int2(this Cardinal c)
        {
            return c switch
            {
                Cardinal.Right => new Vector2Int(1, 0),
                Cardinal.Up => new Vector2Int(0, 1),
                Cardinal.Left => new Vector2Int(-1, 0),
                Cardinal.Down => new Vector2Int(0, -1),
                _ => Vector2Int.zero,
            };
        }

        public static Vector2 Vec2(this Cardinal c)
        {
            return c switch
            {
                Cardinal.Right => new Vector2(1, 0),
                Cardinal.Up => new Vector2(0, 1),
                Cardinal.Left => new Vector2(-1, 0),
                Cardinal.Down => new Vector2(0, -1),
                _ => Vector2.zero,
            };
        }
    }
}
