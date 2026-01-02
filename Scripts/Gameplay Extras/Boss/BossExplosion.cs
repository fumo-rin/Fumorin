using RinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace rinCore
{
    public class BossExplosion
    {
        public class data
        {
            public float delay = 0.35f;
            public float repeatDelay = 0.15f;
            public Vector2 sizeRange = new(0.65f, 1.65f);
            public float maxRadius = 3.5f;
            public float minRadius = 0.35f;
        }
        public BossExplosion(Vector2 origin, int count, data data, Action<Vector2> doWithExplosion)
        {
            IEnumerator CO_Run()
            {
                List<Vector2> relativePositions = new();
                for (int i = 0; i < count; i++)
                {
                    relativePositions.Add(RNG.SeededRandomVector2.ScaleToMagnitude(RNG.FloatRange(data.minRadius, data.maxRadius)));
                }
                if (relativePositions == null || relativePositions.Count <= 0)
                {
                    yield break;
                }
                yield return data.delay.WaitForSeconds();
                foreach (var position in relativePositions)
                {
                    if (doWithExplosion != null)
                    {
                        doWithExplosion?.Invoke(position);
                    }
                    GeneralManager.FunnyExplosion(origin + position);
                    yield return data.repeatDelay.WaitForSeconds();
                }
            }
            CO_Run().RunRoutine();
        }
    }
}
