using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RinCore
{
    #region Helper
    public static partial class Helper
    {

    }
    #endregion
    #region Position collection cast
    public static class VectorCollect
    {
        static Collider2D[] collectNearby;
        public static bool IsNearby<T>(this Vector2 v, LayerMask detectLayer, float radius) where T : MonoBehaviour
        {
            collectNearby = Physics2D.OverlapCircleAll(v, radius, detectLayer);

            foreach (var item in collectNearby)
            {
                if (item.transform == null)
                {
                    continue;
                }
                if (item.transform.TryGetComponent(out T found))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsNearTag(this Vector2 v, LayerMask detectLayer, float radius, string tag)
        {
            return Physics2D.OverlapCircleAll(v, radius, detectLayer)
                .Any(collider => collider.transform != null && collider.transform.CompareTag(tag));
        }
    }
    #endregion
    public static class RectExtensions
    {
        #region Rect Rip From VFavorites
        public static Rect Core_Resize(this Rect rect, float px) { rect.x += px; rect.y += px; rect.width -= px * 2; rect.height -= px * 2; return rect; }

        public static Rect Core_SetPos(this Rect rect, Vector2 v) => rect.Core_SetPos(v.x, v.y);
        public static Rect Core_SetPos(this Rect rect, float x, float y) { rect.x = x; rect.y = y; return rect; }

        public static Rect Core_SetX(this Rect rect, float x) => rect.Core_SetPos(x, rect.y);
        public static Rect Core_SetY(this Rect rect, float y) => rect.Core_SetPos(rect.x, y);
        public static Rect Core_SetXMax(this Rect rect, float xMax) { rect.xMax = xMax; return rect; }
        public static Rect Core_SetYMax(this Rect rect, float yMax) { rect.yMax = yMax; return rect; }

        public static Rect Core_SetMidPos(this Rect r, Vector2 v) => r.Core_SetPos(v).Core_MoveX(-r.width / 2).Core_MoveY(-r.height / 2);

        public static Rect Core_Move(this Rect rect, Vector2 v) { rect.position += v; return rect; }
        public static Rect Core_Move(this Rect rect, float x, float y) { rect.x += x; rect.y += y; return rect; }
        public static Rect Core_MoveX(this Rect rect, float px) { rect.x += px; return rect; }
        public static Rect Core_MoveY(this Rect rect, float px) { rect.y += px; return rect; }

        public static Rect Core_SetWidth(this Rect rect, float f) { rect.width = f; return rect; }
        public static Rect Core_SetWidthFromMid(this Rect rect, float px) { rect.x += rect.width / 2; rect.width = px; rect.x -= rect.width / 2; return rect; }
        public static Rect Core_SetWidthFromRight(this Rect rect, float px) { rect.x += rect.width; rect.width = px; rect.x -= rect.width; return rect; }

        public static Rect Core_SetHeight(this Rect rect, float f) { rect.height = f; return rect; }
        public static Rect Core_SetHeightFromMid(this Rect rect, float px) { rect.y += rect.height / 2; rect.height = px; rect.y -= rect.height / 2; return rect; }
        public static Rect Core_SetHeightFromBottom(this Rect rect, float px) { rect.y += rect.height; rect.height = px; rect.y -= rect.height; return rect; }

        public static Rect Core_AddWidth(this Rect rect, float f) => rect.Core_SetWidth(rect.width + f);
        public static Rect Core_AddWidthFromMid(this Rect rect, float f) => rect.Core_SetWidthFromMid(rect.width + f);
        public static Rect Core_AddWidthFromRight(this Rect rect, float f) => rect.Core_SetWidthFromRight(rect.width + f);

        public static Rect Core_AddHeight(this Rect rect, float f) => rect.Core_SetHeight(rect.height + f);
        public static Rect Core_AddHeightFromMid(this Rect rect, float f) => rect.Core_SetHeightFromMid(rect.height + f);
        public static Rect Core_AddHeightFromBottom(this Rect rect, float f) => rect.Core_SetHeightFromBottom(rect.height + f);

        public static Rect Core_SetSize(this Rect rect, Vector2 v) => rect.Core_SetWidth(v.x).Core_SetHeight(v.y);
        public static Rect Core_SetSize(this Rect rect, float w, float h) => rect.Core_SetWidth(w).Core_SetHeight(h);
        public static Rect Core_SetSize(this Rect rect, float f) { rect.height = rect.width = f; return rect; }

        public static Rect Core_SetSizeFromMid(this Rect r, Vector2 v) => r.Core_Move(r.size / 2).Core_SetSize(v).Core_Move(-v / 2);
        public static Rect Core_SetSizeFromMid(this Rect r, float x, float y) => r.Core_SetSizeFromMid(new Vector2(x, y));
        public static Rect Core_SetSizeFromMid(this Rect r, float f) => r.Core_SetSizeFromMid(new Vector2(f, f));

        public static Rect Core_AlignToPixelGrid(this Rect r) => GUIUtility.AlignRectToDevice(r);
        #endregion
    }
    public static class VectorExtensions
    {
        public static Vector3 ToXZ(this Vector3 v)
        {
            return new()
            {
                x = v.x,
                y = 0f,
                z = v.z,
            };
        }
        public static Vector2 ToX(this Vector2 v)
        {
            return new()
            {
                x = v.x,
                y = 0f
            };
        }
        public static Vector3 Abs(this Vector3 v)
        {
            return new()
            {
                x = Mathf.Abs(v.x),
                y = Mathf.Abs(v.y),
                z = Mathf.Abs(v.z)
            };
        }
        public static Vector3 Multiply(this Vector3 v, float magnitude)
        {
            return new(v.x * magnitude, v.y * magnitude, v.z * magnitude);
        }
        public static Vector2 Multiply(this Vector2 v, float magnitude)
        {
            return new(v.x * magnitude, v.y * magnitude);
        }
        public static Vector3 RoundToInt(this Vector3 v)
        {
            return new Vector3(v.x.Round(), v.y.Round(), v.z.Round());
        }
        public static Vector2 RoundToInt(this Vector2 v)
        {
            return new Vector2(v.x.Round(), v.y.Round());
        }
        public static float DistanceTo(this Vector3 v, Vector3 position)
        {
            return Vector3.Distance(v, position);
        }
        public static float SqDistance(this Vector3 v, Vector3 position)
        {
            return (v - position).sqrMagnitude;
        }
        public static float DistanceTo(this Vector2 v, Vector2 position)
        {
            return Vector2.Distance(v, position);
        }
        public static float SquareDistanceTo(this Vector2 v, Vector2 position)
        {
            return (position - v).sqrMagnitude;
        }
        public static bool SquareDistanceToGreaterThan(this Vector3 v, Vector3 position, float magnitude)
        {
            return (position - v).sqrMagnitude > magnitude * magnitude;
        }
        public static bool SquareDistanceToLessThan(this Vector3 v, Vector3 position, float magnitude)
        {
            return !SquareDistanceToGreaterThan(v, position, magnitude);
        }
        public static bool SquareDistanceToGreaterThan(this Vector2 v, Vector2 position, float magnitude)
        {
            float maxDist = magnitude;

            float dx = position.x - v.x;
            if (Mathf.Abs(dx) > maxDist) return true;

            float dy = position.y - v.y;
            if (Mathf.Abs(dy) > maxDist) return true;

            float sqrDist = dx * dx + dy * dy;
            return sqrDist > maxDist * maxDist;
        }

        public static bool SquareDistanceToLessThan(this Vector2 v, Vector2 position, float magnitude)
        {
            return !SquareDistanceToGreaterThan(v, position, magnitude);
        }
        public static float RandomBetweenXY(this Vector2 v)
        {
            return Random.Range(v.x, v.y);
        }
        public static Vector3 Floor(this Vector3 v)
        {
            return new(v.x.Floor(), v.y.Floor(), v.z.Floor());
        }
        public static Vector3 MoveTowards(this Vector3 v, Vector3 worldPosition, float speed)
        {
            return Vector3.MoveTowards(v, worldPosition, speed * Time.deltaTime);
        }
        public static Vector2 MoveTowards(this Vector2 v, Vector2 worldPosition, float speed)
        {
            return Vector2.MoveTowards(v, worldPosition, speed * Time.deltaTime);
        }
        public static Vector2 LerpTowardsWithDeltaTime(this Vector2 v, Vector2 worldPosition, float speed, float deltaTime)
        {
            return Vector2.Lerp(v, worldPosition, speed * deltaTime);
        }
        public static Vector2 LerpTowards(this Vector2 v, Vector2 worldPosition, float speed)
        {
            return Vector2.Lerp(v, worldPosition, speed * Time.deltaTime);
        }
        public static Vector2 LerpUnclamped(this Vector2 a, Vector2 b, float lerp)
        {
            return a + (b - a) * lerp;
        }
        public static Vector2 LerpEaseInOut01(this Vector2 a, Vector2 b, float lerp) => a.LerpUnclamped(b, lerp.AsEaseInOut01());
        public static Vector3 X(this Vector3 v, float x)
        {
            return new(x, v.y, v.z);
        }
        public static Vector3 Y(this Vector3 v, float y)
        {
            return new(v.x, y, v.z);
        }
        public static Vector3 Z(this Vector3 v, float z)
        {
            return new(v.x, v.y, z);
        }
        public static float CalculateTravelTime(this Vector2 v, float speed)
        {
            return v.magnitude / speed.Max(0.01f);
        }
        public static bool Overlaps(this BoundsInt b, BoundsInt other)
        {
            if (b.Contains(other.min) || b.Contains(other.max))
                return true;
            return false;
        }
        public static Vector2Int GetRandom(this Vector2Int v, Vector2Int min, Vector2Int max)
        {
            return v = new(Random.Range(min.x, max.x), Random.Range(min.y, max.y));
        }
        public static int RandomBetweenXY(this Vector2Int v)
        {
            return Random.Range(v.x, v.y);
        }
        public static Vector2 RandomFromZero(this Vector2 v)
        {
            return new(Random.Range(0f, v.x), Random.Range(0f, v.y));
        }
        public static Vector2Int GetRandom(this Vector2Int v, Vector3Int min, Vector3Int max)
        {
            return GetRandom(v, (Vector2Int)min, (Vector2Int)max);
        }
        public static Vector2 RandomSign(this Vector2 v)
        {
            return new(Random.Range(0, 2) * 2 - 1, Random.Range(0, 2) * 2 - 1);
        }
        public static BoundsInt ToInt(this Bounds b)
        {
            BoundsInt result = new BoundsInt();
            result.min = new(b.min.x.ToInt(), b.min.y.ToInt());
            result.max = new(b.max.x.ToInt(), b.max.y.ToInt());
            return result;
        }
        public static Vector2 RandomCircleDirection(this Vector3 v, float radius, float radiusMax = -1)
        {
            if (radiusMax == -1)
            {
                radiusMax = radius;
            }
            return RandomCircleDirection(v, new Vector2(radius, radiusMax));
        }
        public static Vector2 RandomCircleDirection(this Vector2 v, float radius, float radiusMax = -1)
        {
            Vector3 v3 = (Vector3)v;
            if (radiusMax == -1)
            {
                radiusMax = radius;
            }
            return RandomCircleDirection(v3, new(radius, radiusMax));
        }
        public static Vector2 RandomCircleDirection(this Vector3 v, Vector2 radiusRange)
        {
            Vector2 point = v;
            point += Random.insideUnitCircle.normalized * radiusRange.RandomBetweenXY();
            return point;
        }
        public static Vector2 RandomCircleDirection(this Vector2 v, Vector2 radiusRange)
        {
            Vector2 point = v;
            point += Random.insideUnitCircle.normalized * radiusRange.RandomBetweenXY();
            return point;
        }
        public static Vector2 Rotate2D(this Vector2 v, float angle)
        {
            v = Quaternion.AngleAxis(angle, Vector3.forward) * v;
            return v;
        }
        public static Vector2 RotateTowardsOther(this Vector2 v, Vector2 other, float maxAngleStep)
        {
            float angle = Vector2.SignedAngle(v, other);
            float step = Mathf.Clamp(angle, -maxAngleStep, maxAngleStep);

            Quaternion rotation = Quaternion.AngleAxis(step, Vector3.forward);
            return rotation * v;
        }
        public static float Angle(this Vector2 v, Vector2? other = null)
        {
            Vector2 compare = other ?? Vector2.right;
            return Vector2.SignedAngle(v, compare);
        }
        public static Vector2 Sign(this Vector2 v)
        {
            return new(Mathf.Sign(v.x), Mathf.Sign(v.y));
        }
        public static Vector2 ClampInside(this Vector2 v, Bounds bounds)
        {
            Vector2 vector;
            vector.x = v.x.Clamp(bounds.min.x, bounds.max.x);
            vector.y = v.y.Clamp(bounds.min.y, bounds.max.y);
            return vector;
        }
        public static Vector2 ClampInside(this Vector2 v, Rect rect)
        {
            Vector2 result;
            result.x = Mathf.Clamp(v.x, rect.xMin, rect.xMax);
            result.y = Mathf.Clamp(v.y, rect.yMin, rect.yMax);
            return result;
        }
        public static Vector2 ClampInside2D(this Vector3 v, Bounds bounds)
        {
            return ((Vector2)v).ClampInside(bounds);
        }
        public static Vector2 Bounce(this Vector2 v, Vector2 normal, float bounce)
        {
            return (v.normalized - 2 * (Vector2.Dot(v.normalized, normal)) * normal).normalized * bounce * v.magnitude;
        }
        public static Vector2 Clamp(this Vector2 v, float min, float max)
        {
            return v.normalized * v.magnitude.Clamp(min, max);
        }
        public static Vector2 Squared(this Vector2 v)
        {
            return v * v;
        }
        public static float TravelTime(this Vector2 v, float speed) => v.magnitude / speed;
        public static Vector2 Floor(this Vector2 v) => new(v.x.Floor(), v.y.Floor());
        public static Vector2 Quantize(this Vector2 v, float steps) => (v * steps).Floor() / steps;
        public static Vector2 Absolute(this Vector2 v)
        {
            return new(v.x.Absolute(), v.y.Absolute());
        }
        public static Vector2 ScaleToMagnitude(this Vector2 v, float magnitude)
        {
            Vector2 direction = v.normalized * magnitude;
            return direction;
        }
        public static Vector2 RandomWithin(this Bounds b, Vector2 center)
        {
            Vector2 v = center;
            Vector2 extends = ((Vector2)b.extents);
            v += extends.RandomFromZero() * extends.RandomSign();
            return v;
        }
        public static string BoundsToString(this Bounds b)
        {
            string text = "";
            text += $"x: {b.min.x.ToString()} : {b.max.x.ToString()}##".Color(Color.red).ReplaceLineBreaks("##");
            text += $"y: {b.min.y.ToString()} : {b.max.y.ToString()}".Color(Color.green);
            return text;
        }
        public static Vector2 QuantizeToStepSize(this Vector2 vector, float stepSize = 45f, Rect? biasRect = null)
        {
            if (vector == Vector2.zero)
                return Vector2.zero;

            if (biasRect != null)
                return QuantizeToStepSizeWithBias(vector, biasRect.Value, stepSize);

            float angle = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
            float snappedAngle = Mathf.Round(angle / stepSize) * stepSize;
            float radians = snappedAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        }
        private static Vector2 QuantizeToStepSizeWithBias(this Vector2 vector, Rect biasRect, float stepSize = 45f)
        {
            if (vector == Vector2.zero)
                return Vector2.zero;
            float aspectRatio = biasRect.width / biasRect.height;
            float angle = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
            float adjustedAngle = angle;

            if (aspectRatio > 1f)
                adjustedAngle = Mathf.LerpAngle(angle, Mathf.Round(angle / 180f) * 180f, 0.5f);
            else if (aspectRatio < 1f)
                adjustedAngle = Mathf.LerpAngle(angle, Mathf.Round((angle - 90f) / 180f) * 180f + 90f, 0.5f);

            float snappedAngle = Mathf.Round(adjustedAngle / stepSize) * stepSize;
            float radians = snappedAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        }
        public static void LineChop(this Vector2 a, Vector2 b, int segments, out List<Vector2> result)
        {
            result = new();
            segments = Mathf.Max(2, segments);

            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                result.Add(Vector2.Lerp(a, b, t));
            }
        }
        public static void DrawRect(this Rect r)
        {
            Debug.DrawLine(r.min, r.max, new Color(1f, 1f, 1f, 1f), 1f);
        }
        public static float LowestOfXOrYAbs(this Vector2 v)
        {
            return v.x.Absolute() > v.y.Absolute() ? v.x.Absolute() : v.y.Absolute();
        }
        public static float HighestOfXOrYAbs(this Vector2 v)
        {
            return v.x.Absolute() < v.y.Absolute() ? v.x.Absolute() : v.y.Absolute();
        }
        public static Vector2 RandomPointInBox(this Vector2 v)
        {
            float x = Random.Range(-v.x * 0.5f, v.x * 0.5f);
            float y = Random.Range(-v.y * 0.5f, v.y * 0.5f);
            return new Vector2(x, y);
        }
        public static Rect ToRect(this Vector2 a, Vector2 b)
        {
            Vector2 min = Vector2.Min(a, b);
            Vector2 max = Vector2.Max(a, b);

            Vector2 size = max - min;
            return new Rect(min, size);
        }
        public static bool InBoxDistance(this Vector2 a, Vector2 b, float boxRadius)
        {
            return Mathf.Abs(a.x - b.x) <= boxRadius && Mathf.Abs(a.y - b.y) <= boxRadius;
        }
        public static bool InBoxDistance(this Vector2 a, Vector2 b, float boxRadiusX, float boxRadiusY)
        {
            return Mathf.Abs(a.x - b.x) <= boxRadiusX && Mathf.Abs(a.y - b.y) <= boxRadiusY;
        }
        public static Vector2 Gravity(this Vector2 velocity, float gravity, float maxSpeed, Vector2 direction)
        {
            if (direction == Vector2.zero)
                direction = Vector2.down;
            Vector2 dir = direction.normalized;
            float currentSpeedAlongDir = Vector2.Dot(velocity, dir);
            currentSpeedAlongDir -= gravity * Time.deltaTime;
            currentSpeedAlongDir = Mathf.Max(currentSpeedAlongDir, -Mathf.Abs(maxSpeed));
            Vector2 newVelocity = velocity - Vector2.Dot(velocity, dir) * dir + dir * currentSpeedAlongDir;
            return newVelocity;
        }
    }
}
