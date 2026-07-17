using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace rinCore
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
    #region Pooled Particle single play
    public static partial class ParticleSystemExtensions
    {
        private class PooledParticle
        {
            public ParticleSystem system;
            public bool inUse;
            public int sceneId;
        }
        private static readonly Dictionary<ParticleSystem, List<PooledParticle>> _pool = new();
        private static readonly Dictionary<ParticleSystem, Coroutine> _releaseRoutines = new();
        private static int _activeSceneId;
        [Initialize(-100000)]
        private static void InitParticlePool()
        {
            _activeSceneId = SceneManager.GetActiveScene().buildIndex;

            SceneManager.sceneLoaded += (_, __) =>
            {
                _activeSceneId = SceneManager.GetActiveScene().buildIndex;
                ValidatePools();
            };

            SceneManager.sceneUnloaded += _ =>
            {
                ValidatePools();
            };
        }
        private static void ValidatePools()
        {
            foreach (var kvp in _pool)
            {
                var list = kvp.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var p = list[i];
                    if (p == null || p.system == null || p.sceneId != _activeSceneId)
                    {
                        if (p?.system != null)
                            Object.Destroy(p.system.gameObject);
                        list.RemoveAt(i);
                    }
                }
            }
        }
        public static void PlayCachedOnce(this ParticleSystem prefab, Vector3 position)
        {
            if (prefab == null) return;
            if (!_pool.TryGetValue(prefab, out var list))
                _pool[prefab] = list = new List<PooledParticle>();

            PooledParticle instance = null;

            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].inUse && list[i].system != null)
                {
                    instance = list[i];
                    break;
                }
            }
            if (instance == null)
            {
                var ps = Object.Instantiate(prefab);
                ps.gameObject.SetActive(true);

                instance = new PooledParticle
                {
                    system = ps,
                    sceneId = _activeSceneId,
                    inUse = false
                };

                list.Add(instance);
            }
            instance.inUse = true;
            var system = instance.system;
            var t = system.transform;

            t.position = position;
            t.rotation = prefab.transform.rotation;
            t.localScale = prefab.transform.localScale;

            system.Clear(true);
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play(true);

            if (!_releaseRoutines.TryGetValue(prefab, out var co) || co == null)
                _releaseRoutines[prefab] = Host.StartCoroutine(ReleaseWhenDone(instance));
        }
        private static IEnumerator ReleaseWhenDone(PooledParticle instance)
        {
            var ps = instance.system;
            if (ps == null) yield break;

            while (ps != null && ps.IsAlive(true))
                yield return null;

            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            instance.inUse = false;
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
        public static void EmitSingleParticleCached(this ParticleSystem prefab, Vector3 position, Vector3? velocity = null, float lifetimeSpread = 0f, Color? colorOverride = null, float sizeMultiplier = 1f)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Particle System Extensions - " + nameof(EmitSingleParticleCached) + " called with null prefab.");
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
                startSize = main.startSize.Evaluate() * sizeMultiplier,
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
        public static void RenderAnimatedPoints(this ParticleSystem ps, List<Vector2> positions, float animationLoopsPerSecond, bool staggerPhase = true)
        {
            if (ps == null || positions == null)
                return;

            var tsa = ps.textureSheetAnimation;
            int totalFrames = tsa.numTilesX * tsa.numTilesY;
            float animationDuration = animationLoopsPerSecond <= 0.01f ? 1f : totalFrames / animationLoopsPerSecond;
            int count = positions.Count;

            if (!particleArrayCache.TryGetValue(ps, out var particleArray) || particleArray.Length < count)
            {
                particleArray = new ParticleSystem.Particle[Mathf.Max(count, 128)];
                particleArrayCache[ps] = particleArray;
            }

            var main = ps.main;
            float startSize = main.startSize.constant;
            Color startColor = main.startColor.color;

            for (int i = 0; i < count; i++)
            {
                float animationOffsetSeconds = staggerPhase ? (animationDuration * i / count) : 0f;
                float animationElapsed = Time.time - animationOffsetSeconds;
                animationElapsed %= animationDuration;

                particleArray[i] = new ParticleSystem.Particle
                {
                    position = new Vector3(positions[i].x, positions[i].y, 0f),
                    startLifetime = animationDuration,
                    remainingLifetime = animationDuration - animationElapsed,
                    startColor = startColor,
                    startSize = startSize
                };
            }
            ps.SetParticles(particleArray, count);
        }
        public static void RenderAnimatedPoints_3D(this ParticleSystem ps, List<Vector3> positions, float animationLoopsPerSecond, bool staggerPhase = true)
        {
            if (ps == null || positions == null)
                return;

            var tsa = ps.textureSheetAnimation;
            int totalFrames = tsa.numTilesX * tsa.numTilesY;
            float animationDuration = animationLoopsPerSecond <= 0.01f ? 1f : totalFrames / animationLoopsPerSecond;
            int count = positions.Count;

            if (!particleArrayCache.TryGetValue(ps, out var particleArray) || particleArray.Length < count)
            {
                particleArray = new ParticleSystem.Particle[Mathf.Max(count, 128)];
                particleArrayCache[ps] = particleArray;
            }

            var main = ps.main;
            float startSize = main.startSize.constant;
            Color startColor = main.startColor.color;

            for (int i = 0; i < count; i++)
            {
                float animationOffsetSeconds = staggerPhase ? (animationDuration * i / count) : 0f;
                float animationElapsed = Time.time - animationOffsetSeconds;
                animationElapsed %= animationDuration;

                particleArray[i] = new ParticleSystem.Particle
                {
                    position = new Vector3(positions[i].x, positions[i].y, positions[i].z),
                    startLifetime = animationDuration,
                    remainingLifetime = animationDuration - animationElapsed,
                    startColor = startColor,
                    startSize = startSize
                };
            }
            ps.SetParticles(particleArray, count);
        }
        public static Color32 GetInitialColor32(this ParticleSystem ps)
        {
            var main = ps.main;
            var startColor = main.startColor;

            Color c = startColor.mode switch
            {
                ParticleSystemGradientMode.Color => startColor.color,

                ParticleSystemGradientMode.TwoColors =>
                    Color.Lerp(startColor.colorMin, startColor.colorMax, UnityEngine.Random.value),

                ParticleSystemGradientMode.Gradient =>
                    startColor.gradient.Evaluate(0f),

                ParticleSystemGradientMode.TwoGradients =>
                    Color.Lerp(
                        startColor.gradientMin.Evaluate(0f),
                        startColor.gradientMax.Evaluate(0f),
                        UnityEngine.Random.value
                    ),

                _ => Color.white
            };

            return (Color32)c;
        }
        public static float GetInitialStartSize(this ParticleSystem ps)
        {
            var main = ps.main;
            var startSize = main.startSize;

            return startSize.mode switch
            {
                ParticleSystemCurveMode.Constant => startSize.constant,

                ParticleSystemCurveMode.TwoConstants =>
                    Mathf.Lerp(startSize.constantMin, startSize.constantMax, UnityEngine.Random.value),

                ParticleSystemCurveMode.Curve =>
                    startSize.curve.Evaluate(0f),

                ParticleSystemCurveMode.TwoCurves =>
                    Mathf.Lerp(
                        startSize.curveMin.Evaluate(0f),
                        startSize.curveMax.Evaluate(0f),
                        UnityEngine.Random.value
                    ),

                _ => 1f
            };
        }
    }
}
