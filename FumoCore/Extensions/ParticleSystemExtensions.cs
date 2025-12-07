using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FumoCore.Tools
{
    #region Loot Particle

    public static partial class ParticleSystemExtensions
    {
        private class ParticleData
        {
            public ParticleSystem.Particle particle;
            public Vector3 startPos;
            public float startTime;
            public float duration;

            public ParticleData(Vector3 startPos, float baseTime, float startTimeOffset, Color color, float size, float baseDuration, float durationSpread)
            {
                this.startPos = startPos;
                this.startTime = baseTime + startTimeOffset;

                float spreadAmount = baseDuration * durationSpread / 100f;
                duration = UnityEngine.Random.Range(baseDuration - spreadAmount, baseDuration + spreadAmount);

                particle = new ParticleSystem.Particle
                {
                    position = startPos,
                    startColor = color,
                    startSize = size,
                    startLifetime = duration,
                    remainingLifetime = duration
                };
            }
        }

        private class Batch
        {
            public List<ParticleData> particles;
            public Transform target;
            public float startTime;
            public Batch(IEnumerable<Vector2> positions, Transform target, float startTime, Color color, float size, float duration, float startTimeSpread = 50f, float durationSpread = 50f)
            {
                this.target = target;
                this.startTime = startTime;
                particles = new List<ParticleData>();

                float startTimeSpreadAmount = duration * startTimeSpread / 100f;

                foreach (var pos in positions)
                {
                    float startOffset = Mathf.Max(0f, UnityEngine.Random.Range(-startTimeSpreadAmount, startTimeSpreadAmount));
                    particles.Add(new ParticleData(pos, startTime, startOffset, color, size, duration, durationSpread));
                }
            }
        }
        [Initialize(-999999)]
        private static void RestartLootBatch()
        {
            batchesBySystem = new();
            coroutinesBySystem = new();
            host = null;
        }

        private static Dictionary<ParticleSystem, List<Batch>> batchesBySystem = new();
        private static Dictionary<ParticleSystem, Coroutine> coroutinesBySystem = new();
        private static MonoBehaviour host;
        private static MonoBehaviour Host
        {
            get
            {
                if (host != null && host.gameObject != null) return host;
                var go = new GameObject("[ParticleSystemHost]");
                host = go.AddComponent<MonoBehaviourHost>();
                return host;
            }
        }

        private sealed class MonoBehaviourHost : MonoBehaviour { }

        public static void SpawnParticlesBatch(this ParticleSystem ps, IEnumerable<Vector2> positions, Transform target,
            float duration = 0.5f, Color? color = null, float size = 0.35f,
            float startTimeSpread = 50f, float durationSpread = 50f)
        {
            if (ps == null || target == null) return;

            var col = color ?? Color.white;
            float now = Time.time;

            if (!batchesBySystem.TryGetValue(ps, out var batches))
                batchesBySystem[ps] = batches = new List<Batch>();

            batches.Add(new Batch(positions, target, now, col, size, duration, startTimeSpread, durationSpread));

            if (!coroutinesBySystem.TryGetValue(ps, out var co) || co == null)
                coroutinesBySystem[ps] = Host.StartCoroutine(UpdateCoroutine(ps));
        }

        private static IEnumerator UpdateCoroutine(ParticleSystem ps)
        {
            while (true)
            {
                if (ps == null || ps.gameObject == null)
                {
                    batchesBySystem.Remove(ps);
                    coroutinesBySystem.Remove(ps);
                    yield break;
                }

                if (!batchesBySystem.TryGetValue(ps, out var batches) || batches.Count == 0)
                    break;

                float now = Time.time;
                List<ParticleSystem.Particle> allParticles = new();

                for (int b = batches.Count - 1; b >= 0; b--)
                {
                    var batch = batches[b];
                    Vector3 targetPos = batch.target.position;
                    bool finished = true;

                    for (int i = batch.particles.Count - 1; i >= 0; i--)
                    {
                        var data = batch.particles[i];
                        float t = Mathf.Clamp01((now - data.startTime) / data.duration);
                        t = Mathf.SmoothStep(0, 1, t);

                        if (t >= 1f)
                        {
                            batch.particles.RemoveAt(i);
                            continue;
                        }

                        data.particle.position = Vector3.Lerp(data.startPos, targetPos, t);
                        data.particle.remainingLifetime = data.duration - (now - data.startTime);
                        batch.particles[i] = data;
                        allParticles.Add(data.particle);
                        finished = false;
                    }

                    if (finished)
                        batches.RemoveAt(b);
                }

                ps.SetParticles(allParticles.ToArray(), allParticles.Count);
                yield return null;
            }

            ps.Clear();
            batchesBySystem.Remove(ps);
            coroutinesBySystem.Remove(ps);
        }
    }

    #endregion
    public static partial class ParticleSystemExtensions
    {
        public static void PlayIfNotPlaying(this ParticleSystem ps)
        {
            if (ps.isPlaying) return;
            ps.Play();
        }
        private static readonly Dictionary<ParticleSystem, ParticleSystem> particleCache = new();
        public static void EmitSingleCached(this ParticleSystem prefab, Vector3 position, Vector3? velocity = null, float lifetimeSpread = 0f, Color? colorOverride = null)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Particle System Extensions - " + nameof(EmitSingleCached) + " called with null prefab.");
                return;
            }

            if (!particleCache.TryGetValue(prefab, out var cached) || cached == null)
            {
                cached = GameObject.Instantiate(prefab);
                particleCache[prefab] = cached;
            }

            if (!cached.gameObject.activeInHierarchy)
                cached.gameObject.SetActive(true);

            var main = prefab.main;

            float baseLifetime = main.startLifetime.Evaluate();
            float finalLifetime = baseLifetime.Spread(lifetimeSpread);

            var emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity ?? Vector3.zero,
                startColor = colorOverride ?? main.startColor.Evaluate(),
                startSize = main.startSize.Evaluate(),
                startLifetime = finalLifetime,
            };

            cached.Emit(emitParams, 1);
        }
        private static float Evaluate(this ParticleSystem.MinMaxCurve curve)
        {
            return curve.mode switch
            {
                ParticleSystemCurveMode.Constant => curve.constant,
                ParticleSystemCurveMode.TwoConstants => UnityEngine.Random.Range(curve.constantMin, curve.constantMax),
                ParticleSystemCurveMode.Curve => curve.curve.Evaluate(UnityEngine.Random.value),
                ParticleSystemCurveMode.TwoCurves =>
                    Mathf.Lerp(curve.curveMin.Evaluate(UnityEngine.Random.value),
                               curve.curveMax.Evaluate(UnityEngine.Random.value),
                               UnityEngine.Random.value),
                _ => 1f
            };
        }
        private static Color Evaluate(this ParticleSystem.MinMaxGradient gradient)
        {
            return gradient.mode switch
            {
                ParticleSystemGradientMode.Color => gradient.color,
                ParticleSystemGradientMode.TwoColors => UnityEngine.Color.Lerp(gradient.colorMin, gradient.colorMax, UnityEngine.Random.value),
                ParticleSystemGradientMode.Gradient => gradient.gradient.Evaluate(UnityEngine.Random.value),
                ParticleSystemGradientMode.TwoGradients =>
                    Color.Lerp(gradient.gradientMin.Evaluate(UnityEngine.Random.value),
                               gradient.gradientMax.Evaluate(UnityEngine.Random.value),
                               UnityEngine.Random.value),
                _ => Color.white
            };
        }
        [Initialize(-10000)]
        public static void InitializeParticleExtensions()
        {
            foreach (var ps in particleCache.Values)
            {
                if (ps != null)
                    Object.Destroy(ps.gameObject);
            }
            particleCache.Clear();

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ParticleSystemExtensions.RevalidateCache();
        }
        private static void OnSceneUnloaded(Scene scene)
        {
            ParticleSystemExtensions.RevalidateCache();
        }
        public static void RevalidateCache()
        {
            var invalidParticleKeys = particleCache
                .Where(kvp => kvp.Key == null || kvp.Value == null)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in invalidParticleKeys)
                particleCache.Remove(key);

            foreach (var kvp in particleCache.ToList())
            {
                if (kvp.Value == null || kvp.Value.gameObject == null)
                {
                    particleCache.Remove(kvp.Key);
                }
            }

            var invalidArrayKeys = particleArrayCache
                .Where(kvp => kvp.Key == null)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in invalidArrayKeys)
                particleArrayCache.Remove(key);
        }
        private static readonly Dictionary<ParticleSystem, ParticleSystem.Particle[]> particleArrayCache = new();
        public static void RenderAnimatedPoints(this ParticleSystem ps, List<Vector2> positions, float animationDuration, bool staggerPhase = true)
        {
            if (ps == null || positions == null) return;

            int count = positions.Count;

            if (!particleArrayCache.TryGetValue(ps, out var particleArray) || particleArray.Length < count)
            {
                particleArray = new ParticleSystem.Particle[Mathf.Max(count, 64)];
                particleArrayCache[ps] = particleArray;
            }

            ParticleSystem.MainModule main = ps.main;
            Color startColor = main.startColor.color;
            float startSize = main.startSize.constant;

            float currentTime = Time.time;
            float baseRotationRad = main.startRotation.constant;
            float baseRotationDeg = -baseRotationRad * Mathf.Rad2Deg;

            for (int i = 0; i < count; i++)
            {
                if (particleArray[i].remainingLifetime <= 0f || particleArray[i].startLifetime != animationDuration)
                {
                    particleArray[i].startColor = startColor;
                    particleArray[i].startSize = startSize;
                    particleArray[i].startLifetime = animationDuration;
                    particleArray[i].rotation3D = new Vector3(0f, 0f, baseRotationDeg);
                    particleArray[i].velocity = Vector3.zero;
                }

                particleArray[i].position = new Vector3(positions[i].x, positions[i].y, 0f);

                float phaseOffset = 0f;
                if (staggerPhase && count > 1)
                    phaseOffset = (animationDuration / count) * i;

                float timeInCycle = (currentTime + phaseOffset) % animationDuration;
                float remainingLifetime = animationDuration - timeInCycle;

                remainingLifetime = Mathf.Max(0.001f, remainingLifetime);

                particleArray[i].remainingLifetime = remainingLifetime;
            }

            ps.SetParticles(particleArray, count, 0);
            if (!ps.isPlaying)
                ps.Play();
        }
    }
}
