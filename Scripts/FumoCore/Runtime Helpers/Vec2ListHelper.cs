using UnityEngine;
using System;
using System.Collections.Generic;
namespace rinCore
{
    public static partial class RinHelper
    {
        #region Curved Line
        public static partial class Vec2List
        {
            /// <summary>
            /// Generates a curved 2D line from start to end with curvature and side bias.
            /// Can generate only a fraction of the curve using tMax (0..1).
            /// </summary>
            /// <param name="start">Start point</param>
            /// <param name="end">End point</param>
            /// <param name="curvature">Curve intensity relative to distance (0 = straight line)</param>
            /// <param name="leftBias">True = curve to left, False = curve to right</param>
            /// <param name="segments">Number of points along the curve</param>
            /// <param name="tMax">Fraction of the curve to generate (0..1)</param>
            /// <param name="minDistance">Minimum distance to apply curve</param>
            /// <returns>List of Vector2 points along the curve</returns>
            public static List<Vector2> CurvedLineLerp(Vector2 start, Vector2 end, float curvature, bool leftBias, int segments = 20, float tMax = 1f, float minDistance = 0.01f)
            {
                List<Vector2> points = new List<Vector2>();

                float distance = Vector2.Distance(start, end);
                if (distance < minDistance || tMax <= 0f)
                {
                    points.Add(start);
                    return points;
                }
                tMax = Mathf.Clamp01(tMax);
                Vector2 dirNormalized = (end - start) / distance;
                Vector2 perp = leftBias ? new Vector2(-dirNormalized.y, dirNormalized.x) : new Vector2(dirNormalized.y, -dirNormalized.x);

                Vector2 midpoint = start + (end - start) * 0.5f;
                Vector2 control = midpoint + perp * curvature * distance;

                int steps = Mathf.Max(1, segments);
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps * tMax;
                    Vector2 point = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * control + Mathf.Pow(t, 2) * end;
                    points.Add(point);
                }

                return points;
            }
        }
        #endregion
        public static partial class Vec2List
        {
            /// <summary>
            /// Equilateral triangle (3 points), normalized to unit circle.
            /// </summary>
            public static List<Vector2> Triangle3(float size, out List<Vector2> points)
            {
                points = new List<Vector2>
            {
                new Vector2(0f, 1f),                  // Top
                new Vector2(-0.8660254f, -0.5f),      // Bottom left  (-√3/2, -0.5)
                new Vector2(0.8660254f, -0.5f)        // Bottom right (√3/2, -0.5)
            };

                Scale(points, size);
                return points;
            }

            /// <summary>
            /// Square (4 points), normalized to unit circle.
            /// </summary>
            public static List<Vector2> Square4(float size, out List<Vector2> points)
            {
                points = new List<Vector2>
            {
                new Vector2(-0.7071068f, -0.7071068f), // (-√2/2, -√2/2)
                new Vector2(0.7071068f, -0.7071068f),
                new Vector2(0.7071068f, 0.7071068f),
                new Vector2(-0.7071068f, 0.7071068f)
            };

                Scale(points, size);
                return points;
            }

            /// <summary>
            /// Regular pentagon (5 points), normalized to unit circle.
            /// </summary>
            public static List<Vector2> Pentagon5(float size, out List<Vector2> points)
            {
                points = new List<Vector2>
            {
                new Vector2(0f, 1f),
                new Vector2(0.9510565f, 0.3090170f),
                new Vector2(0.5877853f, -0.8090170f),
                new Vector2(-0.5877853f, -0.8090170f),
                new Vector2(-0.9510565f, 0.3090170f)
            };

                Scale(points, size);
                return points;
            }

            /// <summary>
            /// Regular hexagon (6 points), normalized to unit circle.
            /// </summary>
            public static List<Vector2> Hexagon6(float size, out List<Vector2> points)
            {
                points = new List<Vector2>
            {
                new Vector2(0f, 1f),
                new Vector2(0.8660254f, 0.5f),
                new Vector2(0.8660254f, -0.5f),
                new Vector2(0f, -1f),
                new Vector2(-0.8660254f, -0.5f),
                new Vector2(-0.8660254f, 0.5f)
            };

                Scale(points, size);
                return points;
            }

            /// <summary>
            /// Generic polygon (N points), normalized to unit circle.
            /// </summary>
            public static List<Vector2> PolygonN(int n, float size, float rotationDegrees, out List<Vector2> points)
            {
                points = new List<Vector2>();
                float rotationRad = rotationDegrees * MathF.PI / 180f;
                for (int i = 0; i < n; i++)
                {
                    float angle = i * MathF.PI * 2f / n - MathF.PI / 2f + rotationRad;

                    points.Add(new Vector2(MathF.Cos(angle), MathF.Sin(angle)));
                }
                Scale(points, size);
                return points;
            }
            /// <summary>
            /// Helper to apply size scaling.
            /// </summary>
            private static void Scale(List<Vector2> points, float size)
            {
                for (int i = 0; i < points.Count; i++)
                    points[i] *= size;
            }
            public struct SpiralData
            {
                public List<Vector2> points;
                public List<Vector2> tangents;
                public float angleStep; // radians per step
                public struct spiralSegment
                {
                    public Vector2 point, tangent;
                }
                public IEnumerable<spiralSegment> Sequence()
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        yield return new spiralSegment()
                        {
                            point = points[i],
                            tangent = tangents[i],
                        };
                    }
                }
            }

            /// <summary>
            /// Generates an Archimedean spiral with optional start radius offset and angle offset (spin).
            /// </summary>
            /// <param name="pointCount">Number of points.</param>
            /// <param name="pointsPerRevolution">Points per full 360° revolution.</param>
            /// <param name="size">Final radius of the spiral.</param>
            /// <param name="startRadius">Offset radius at which the spiral starts.</param>
            /// <param name="angleOffsetDegrees">Initial rotation of the spiral in degrees.</param>
            /// <param name="flip">Clockwise or counter-clockwise.</param>
            /// <returns>SpiralData containing points and tangent directions.</returns>
            public static SpiralData GenerateSpiral(
                int pointCount,
                int pointsPerRevolution,
                float size,
                float startRadius = 0f,
                float angleOffsetDegrees = 0f,
                bool flip = false)
            {
                List<Vector2> points = new List<Vector2>(pointCount);
                List<Vector2> tangents = new List<Vector2>(pointCount);

                float angleStep = (2f * Mathf.PI) / pointsPerRevolution;
                float direction = flip ? -1f : 1f;

                for (int i = 0; i < pointCount; i++)
                {
                    float t = i / (float)(pointCount - 1); // 0 → 1
                    float radius = startRadius + t * (size - startRadius);

                    float angle = i * angleStep * direction;
                    Vector2 p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    points.Add(p);

                    Vector2 tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * radius
                                      + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ((size - startRadius) / (pointCount - 1));
                    tangents.Add(tangent.normalized);
                }

                float radOffset = angleOffsetDegrees * Mathf.Deg2Rad;
                float cos = Mathf.Cos(radOffset);
                float sin = Mathf.Sin(radOffset);

                for (int i = 0; i < pointCount; i++)
                {
                    Vector2 p = points[i];
                    points[i] = new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);

                    Vector2 t = tangents[i];
                    tangents[i] = new Vector2(t.x * cos - t.y * sin, t.x * sin + t.y * cos);
                }

                return new SpiralData
                {
                    points = points,
                    tangents = tangents,
                    angleStep = angleStep
                };
            }
        }
    }
}