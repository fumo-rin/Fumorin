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
        public static void HandleStepClimbing(this BoxCollider box, Rigidbody rb, Vector3 moveDir, float maxStepHeight, float maxSlopeAngle = 45f)
        {
            if (moveDir.sqrMagnitude < 0.001f) return;

            Vector3 worldCenter = box.transform.TransformPoint(box.center);
            float extentsY = (box.size.y * 0.5f) * box.transform.lossyScale.y;
            Vector3 bottomPoint = worldCenter - new Vector3(0, extentsY, 0);
            Vector3 direction = new Vector3(moveDir.x, 0, moveDir.z).normalized;
            float contactCheckDistance = (box.size.z * 0.5f * box.transform.lossyScale.z) + 0.15f;
            Vector3 lowerOrigin = bottomPoint + Vector3.up * 0.02f;
            if (Physics.Raycast(lowerOrigin, direction, out RaycastHit hitLower, contactCheckDistance))
            {
                float surfaceAngle = Vector3.Angle(Vector3.up, hitLower.normal);
                if (surfaceAngle <= maxSlopeAngle) return;
                Vector3 upperOrigin = bottomPoint + Vector3.up * maxStepHeight;
                if (!Physics.Raycast(upperOrigin, direction, contactCheckDistance))
                {
                    Vector3 downwardRayOrigin = upperOrigin + (direction * contactCheckDistance);
                    if (Physics.Raycast(downwardRayOrigin, Vector3.down, out RaycastHit hitStepTop, maxStepHeight))
                    {
                        float actualStepHeight = hitStepTop.point.y - bottomPoint.y;
                        float topSurfaceAngle = Vector3.Angle(Vector3.up, hitStepTop.normal);
                        if (topSurfaceAngle > maxSlopeAngle) return;
                        if (actualStepHeight > 0.02f && actualStepHeight <= maxStepHeight)
                        {
                            rb.position += Vector3.up * actualStepHeight;
                            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                        }
                    }
                }
            }
        }
    }
    #endregion
}