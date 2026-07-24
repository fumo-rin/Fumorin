using System;
using UnityEngine;

namespace rinCore
{
    #region Raycast Helper
    public static partial class RinHelper
    {
        public static Ray RayLerp(Ray a, Ray b, float lerp01)
        {
            lerp01 = lerp01.Clamp(0f, 1f);
            Vector3 origin = a.origin.LerpUnclamped(b.origin, lerp01);
            Vector3 direction = a.direction.SlerpUnclamped(b.direction, lerp01).normalized;
            return new Ray(origin, direction);
        }
        public static Ray RayDot(Ray r, float rngDot)
        {
            Ray result = r;
            Vector3 forward = r.direction.normalized;
            float dot = 1f - RNG.FloatRange(0f, 1f) * rngDot;
            Vector3 rng = RNG.SeededRandomInsideUnitSphere;
            Vector3 tangent = Vector3.ProjectOnPlane(rng, forward);

            if (tangent.sqrMagnitude < 0.000001f)
                tangent = Vector3.Cross(forward, Vector3.up);

            tangent.Normalize();
            float tangentScale = Mathf.Sqrt(1f - dot * dot);
            result.direction = (forward * dot + tangent * tangentScale).ScaleToMagnitude(r.direction.magnitude);

            return result;
        }
    }
    [System.Serializable]
    public struct RinRaycast
    {
        public Ray ray;
        public LayerMask mask;
        [field: SerializeField] public float distance { get; private set; }
        public QueryTriggerInteraction TI;
        private Action<RinRaycast, RaycastHit, float> _onHit;
        public RinRaycast(Ray r, LayerMask mask, float distance, QueryTriggerInteraction TI)
        {
            ray = r;
            this.mask = mask;
            this.distance = distance;
            this.TI = TI;
            _onHit = null;
        }
        public RinRaycast With(Action<RinRaycast, RaycastHit, float> callback)
        {
            _onHit += callback;
            return this;
        }
        public bool Cast(float damage)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, distance, mask, TI))
            {
                _onHit?.Invoke(this, hit, damage);
                return true;
            }

            return false;
        }
        public bool Cast<T>(out RaycastHit hit, out T item, float damage, bool withTransformRoot = false)
        {
            item = default;

            if (Physics.Raycast(ray, out hit, distance, mask, TI))
            {
                _onHit?.Invoke(this, hit, damage);

                if (hit.transform.TryGetComponent(out item) ||
                    (withTransformRoot && hit.transform.root.TryGetComponent(out item)))
                {
                    return item != null;
                }
            }
            return false;
        }
    }
    public static partial class Physics3DExtensions
    {

    }
    #endregion
    #region Hitscan Impact
    #region Impact Struct
    public readonly struct Impact
    {
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly float Speed;
        public Vector3 Velocity => Direction * Speed;
        public Impact(Vector3 point, Vector3 direction, float speed)
        {
            Point = point;
            Direction = direction.normalized;
            Speed = speed;
        }
        public Impact(RaycastHit hit, Ray ray, float speed)
            : this(hit.point, ray.direction, speed)
        {
        }
    }
    #endregion
    public static partial class Physics3DExtensions
    {
        public static bool AddImpactVelocity(this Collider collider, Impact impact, float strength = 1f) =>
            collider.AddImpactVelocity(impact.Velocity, impact.Point, strength);

        private const float SoftVelocityLimit = 6f;
        private const float HardVelocityLimit = 11f;
        public static bool AddImpactVelocity(this Collider collider, Vector3 impactVelocity, Vector3 hitPoint, float strength = 1f)
        {
            if (!collider.TryGetComponent(out Rigidbody rb) || rb.isKinematic)
                return false;

            float speed = rb.linearVelocity.magnitude;
            float scale = 1f;
            if (rb.mass < 1f)
            {
                scale /= rb.mass.Clamp(0.33f, 1f);
            }
            if (speed > SoftVelocityLimit)
            {
                scale = Mathf.Sqrt(SoftVelocityLimit / speed);
                if (speed > HardVelocityLimit)
                {
                    float t = speed / HardVelocityLimit;
                    scale /= t * t;
                }
            }
            Vector3 impulse = impactVelocity * rb.mass * strength * scale;
            rb.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
            return true;
        }
    }
    #endregion
    #region Staircase Stepsize
    public static partial class Physics3DExtensions
    {
        /// <summary>
        /// Attempts to climb steps ahead.
        /// </summary>
        /// <returns>True if a step was successfully climbed this frame, false otherwise.</returns>
        public static bool HandleStepClimbing(this BoxCollider box, Rigidbody rb, Vector3 moveDir, float maxStepHeight, Vector3 groundNormal, float maxSlopeAngle = 45f)
        {
            Vector3 xzVel = new Vector3(moveDir.x, 0f, moveDir.z);
            if (xzVel.sqrMagnitude < 0.001f) return false;

            float speed = xzVel.magnitude;
            Vector3 direction = xzVel.normalized;

            Vector3 worldCenter = box.transform.TransformPoint(box.center);
            float extentsY = (box.size.y * 0.5f) * box.transform.lossyScale.y;
            Vector3 bottomPoint = worldCenter - new Vector3(0f, extentsY, 0f);

            float checkDistance = Mathf.Max(speed * Time.fixedDeltaTime, 0.22f);

            // Detect foot level obstacle
            if (Physics.BoxCast(worldCenter + Vector3.up * 0.02f, box.size * 0.48f, direction, out RaycastHit hitLower, rb.rotation, checkDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                // Ignore perfectly flat ground (< 1 degree)
                float lowerHitAngle = Vector3.Angle(Vector3.up, hitLower.normal);
                if (lowerHitAngle < 1.0f) return false;

                // Check for headroom above the step
                Vector3 upperBoxCenter = worldCenter + Vector3.up * (maxStepHeight + 0.05f);
                if (!Physics.BoxCast(upperBoxCenter, box.size * 0.48f, direction, rb.rotation, checkDistance + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    Vector3 stepCheckOrigin = hitLower.point + direction * 0.05f + Vector3.up * (maxStepHeight + 0.05f);
                    if (Physics.Raycast(stepCheckOrigin, Vector3.down, out RaycastHit hitStepTop, maxStepHeight + 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        // Step top must be flat enough to stand on (<= 45 deg)
                        float topSurfaceAngle = Vector3.Angle(Vector3.up, hitStepTop.normal);
                        if (topSurfaceAngle > maxSlopeAngle) return false;
                        // Calculate the step height relative to the player's feet
                        float stepTopY = hitStepTop.point.y;
                        float actualStepHeight = stepTopY - bottomPoint.y;

                        if (actualStepHeight > 0.01f && actualStepHeight <= maxStepHeight)
                        {
                            rb.position = new Vector3(rb.position.x, stepTopY + 0.01f, rb.position.z) + direction * 0.03f;
                            if (rb.linearVelocity.y < 0.1f)
                            {
                                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0.1f, rb.linearVelocity.z);
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
    #endregion
}