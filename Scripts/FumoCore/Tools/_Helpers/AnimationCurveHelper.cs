using System.Collections;
using UnityEngine;

namespace RinCore
{
    public static partial class RinHelper
    {
        public static AnimationCurve InitializedAnimationCurve => new AnimationCurve().Initialized();
        public static AnimationCurve Initialized(this AnimationCurve a)
        {
            return new AnimationCurve()
            {
                keys = new Keyframe[2]
                {
                    new Keyframe(0f,1f),
                    new Keyframe(1f,1f),
                }
            };
        }
    }
}