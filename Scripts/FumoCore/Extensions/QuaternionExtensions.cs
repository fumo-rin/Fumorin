using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace rinCore
{
    public static class QuaternionExtensions
    {
        public static Quaternion LerpTowards(this Quaternion q, Quaternion target, float delta)
        {
            return Quaternion.Lerp(q, target, delta * Time.deltaTime);
        }
        public static Quaternion LerpTowards(this Quaternion q, Vector3 target, float delta)
        {
            return Quaternion.Lerp(q, Quaternion.Euler(target.normalized), delta * Time.deltaTime);
        }
        /// <summary>
        /// Converts a 2D direction vector or Euler angle into a 2D Z-axis Quaternion rotation.
        /// </summary>
        /// <param name="direction">The direction vector (e.g. Vector2.up, normal) or Euler values.</param>
        /// <param name="forward">The default zero-angle orientation vector (defaults to Vector2.right / 0 deg).</param>
        /// <param name="isEuler">If true, treats X and Y as direct degree offsets instead of a direction vector.</param>
        public static Quaternion ToRotation2D(this Vector2 direction, Vector2? forward = null, bool isEuler = false)
        {
            if (isEuler)
                return Quaternion.Euler(direction.x, direction.y, 0f);

            Vector2 baseForward = forward ?? Vector2.right;

            if (direction.sqrMagnitude < 0.0001f || baseForward.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            return Quaternion.FromToRotation(baseForward, direction);
        }
    }
}
