using RinCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace RinCore
{
    #region Play Mode
    public partial class MusicPlayer
    {
        private bool isFading = false;
        public const string PlaymodePrefsKey = "PlayMode";
        [SerializeField] MusicRoomTracklist shufflePlaylist;
        static PlayMode currentPlayMode = PlayMode.None;
        public enum PlayMode
        {
            None = 0,
            Shuffle = 1,
            Loop = 2
        }
        public static void SetPlayMode(PlayMode mode)
        {
            PlayMode lastMode = currentPlayMode;
            currentPlayMode = mode;
            switch (currentPlayMode)
            {
                case PlayMode.None:
                    break;
                case PlayMode.Shuffle:
                    QueueShuffleTrack();
                    if (!IsPlaying && Playlist.Count <= 0)
                    {
                        if (Playlist.TryDequeue(out MusicWrapper w))
                        {
                            w.Play();
                        }
                    }
                    if (lastMode != PlayMode.Shuffle)
                    {
                        FadeOutAndWait();
                    }
                    break;
                case PlayMode.Loop:
                    if (lastMode != PlayMode.Loop)
                    {
                        StartPlayingLoopedMusic();
                    }
                    Playlist.Clear();
                    break;
                default:
                    break;
            }
        }
        public static bool QueueShuffleTrack()
        {
            if (currentPlayMode == PlayMode.Shuffle)
            {
                return instance.shufflePlaylist.QueueRandomTrack(in Playlist);
            }
            return false;
        }
        public static PlayMode FetchPlaymode()
        {
            PlayMode mode = PlayMode.Loop;
            if (PlayerPrefs.HasKey(PlaymodePrefsKey))
            {
                mode = (PlayMode)(PlayerPrefs.GetInt(PlaymodePrefsKey, 0));
            }
            return mode;
        }
        public static void StartPlayingLoopedMusic()
        {
            if (loopedMusic == null)
            {
                return;
            }
            if (instance == null)
            {
                return;
            }
            if (instance is MusicPlayer p)
            {
                p.track1.loop = true;
                p.track2.loop = true;
            }
            loopedMusic.Play();
        }
    }
    #endregion
    #region Beat
    public partial class MusicPlayer
    {
        public static event System.Action WhenBeat;

        private static double nextBeatDspTime = 0;
        private static bool beatActive => IsPlaying;

        private static float dynamicBpm = 0f;
        private static float dynamicBeatLength = 0.5f;

        private const float bpmRefreshRate = 0.12f;       // seconds between analyses
        private const float bpmSmoothAlpha = 0.25f;       // smoothing for BPM (0..1)
        private const float phaseCorrectionGain = 0.65f;  // how strongly to correct beat phase (0..1)
        private const float onsetThreshold = 1e-6f;      // threshold for onset flux acceptance
        private const bool debug = false;

        private float bpmUpdateTimer = 0f;

        private void RunBeat()
        {
            if (!beatActive || instance == null) return;

            var playingSource = instance.selectedTrack == 1 ? instance.track1 : instance.track2;
            var wrapper = currentlyPlaying.music;
            if (!playingSource.isPlaying || wrapper == null)
                return;

            bpmUpdateTimer += Time.unscaledDeltaTime;
            if (bpmUpdateTimer >= bpmRefreshRate)
            {
                bpmUpdateTimer = 0f;
                UpdateDynamicBpmAndPhase(playingSource, wrapper);
            }

            double dsp = AudioSettings.dspTime;
            if (dsp >= nextBeatDspTime && !isFading)
            {
                WhenBeat?.Invoke();
                nextBeatDspTime += dynamicBeatLength;
            }
        }

        private static void UpdateDynamicBpmAndPhase(AudioSource source, MusicWrapper wrapper)
        {
            AudioClip clip = source.clip;
            if (clip == null) return;

            int freq = clip.frequency;

            int analysisWindowSamples = 4096;
            int hopSizeSamples = analysisWindowSamples / 4;

            int currentSample = source.timeSamples;
            if (currentSample < 0 || currentSample >= clip.samples) return;

            int startSample = Mathf.Clamp(currentSample - analysisWindowSamples / 2, 0, Mathf.Max(0, clip.samples - analysisWindowSamples));
            float[] raw = new float[analysisWindowSamples];
            clip.GetData(raw, startSample);

            if (clip.channels == 2)
            {
                float[] rawStereo = new float[analysisWindowSamples * 2];
                clip.GetData(rawStereo, startSample);
                for (int i = 0; i < analysisWindowSamples; i++)
                    raw[i] = 0.5f * (rawStereo[i * 2] + rawStereo[i * 2 + 1]);
            }

            int frameSize = 512;
            int frameHop = 256;
            int frames = Mathf.Max(1, (analysisWindowSamples - frameSize) / frameHop);

            float[] energy = new float[frames];
            for (int f = 0; f < frames; f++)
            {
                int baseIdx = f * frameHop;
                float e = 0f;
                for (int i = 0; i < frameSize; i++)
                {
                    float s = raw[baseIdx + i];
                    e += s * s;
                }
                energy[f] = e;
            }

            float[] flux = new float[frames];
            for (int i = 1; i < frames; i++)
            {
                float d = energy[i] - energy[i - 1];
                flux[i] = Mathf.Max(0f, d);
            }

            float maxFlux = 0f;
            for (int i = 0; i < frames; i++) if (flux[i] > maxFlux) maxFlux = flux[i];
            if (maxFlux > 0f)
            {
                for (int i = 0; i < frames; i++) flux[i] /= maxFlux;
            }

            float secondsPerFrame = (float)frameHop / freq;
            int minBpm = 40;
            int maxBpm = 240;

            int minLag = Mathf.Max(2, Mathf.FloorToInt((60f / maxBpm) / secondsPerFrame));
            int maxLag = Mathf.Max(minLag + 1, Mathf.CeilToInt((60f / minBpm) / secondsPerFrame));

            if (frames < maxLag + 2)
            {
                ApplyBpmFallback(wrapper);
                return;
            }

            float bestCorr = 0f;
            int bestLag = minLag;

            for (int lag = minLag; lag <= maxLag; lag++)
            {
                float corr = 0f;
                float denomA = 0f;
                float denomB = 0f;
                for (int i = 0; i < frames - lag; i++)
                {
                    corr += flux[i] * flux[i + lag];
                    denomA += flux[i] * flux[i];
                    denomB += flux[i + lag] * flux[i + lag];
                }
                float norm = (denomA > 0f && denomB > 0f) ? corr / Mathf.Sqrt(denomA * denomB) : 0f;

                if (norm > bestCorr)
                {
                    bestCorr = norm;
                    bestLag = lag;
                }
            }

            float secondsPerBeat = bestLag * secondsPerFrame;
            if (secondsPerBeat <= 0f)
            {
                ApplyBpmFallback(wrapper);
                return;
            }

            float bpmCandidate = 60f / secondsPerBeat;

            while (bpmCandidate < 60f) bpmCandidate *= 2f;
            while (bpmCandidate > 180f) bpmCandidate *= 0.5f;

            if (bestCorr < 0.08f)
            {
                if (wrapper.bpm > 0f)
                {
                    bpmCandidate = wrapper.bpm;
                }
                dynamicBpm = dynamicBpm <= 0f ? bpmCandidate : Mathf.Lerp(dynamicBpm, bpmCandidate, bpmSmoothAlpha * 0.4f);
            }
            else
            {
                dynamicBpm = dynamicBpm <= 0f ? bpmCandidate : Mathf.Lerp(dynamicBpm, bpmCandidate, bpmSmoothAlpha);
            }

            dynamicBpm = Mathf.Clamp(dynamicBpm, 40f, 220f);
            dynamicBeatLength = 60f / dynamicBpm;

            int bestOnsetFrame = 0;
            float bestOnsetVal = 0f;
            for (int i = 0; i < frames; i++)
            {
                if (flux[i] > bestOnsetVal)
                {
                    bestOnsetVal = flux[i];
                    bestOnsetFrame = i;
                }
            }

            if (bestOnsetVal > onsetThreshold)
            {
                int onsetSampleIndex = startSample + bestOnsetFrame * frameHop;

                double dspNow = AudioSettings.dspTime;
                currentSample = source.timeSamples;
                double onsetDsp = dspNow + ((double)onsetSampleIndex - (double)currentSample) / freq;
                if (nextBeatDspTime <= 0)
                {
                    nextBeatDspTime = onsetDsp + dynamicBeatLength;
                }
                else
                {
                    double phaseError = onsetDsp - nextBeatDspTime;
                    double halfBeat = dynamicBeatLength * 0.5;
                    if (phaseError > halfBeat) phaseError -= dynamicBeatLength * System.Math.Floor((phaseError / dynamicBeatLength) + 0.5);
                    if (phaseError < -halfBeat) phaseError += dynamicBeatLength * System.Math.Floor(((-phaseError) / dynamicBeatLength) + 0.5);

                    nextBeatDspTime += phaseCorrectionGain * phaseError;
                }
            }
        }

        private static void ApplyBpmFallback(MusicWrapper wrapper)
        {
            if (wrapper != null && wrapper.bpm > 0f)
            {
                dynamicBpm = Mathf.Lerp(dynamicBpm <= 0f ? wrapper.bpm : dynamicBpm, wrapper.bpm, bpmSmoothAlpha);
                dynamicBeatLength = 60f / dynamicBpm;
            }
            else
            {
                dynamicBpm = Mathf.Lerp(dynamicBpm <= 0f ? 120f : dynamicBpm, 120f, bpmSmoothAlpha);
                dynamicBeatLength = 60f / dynamicBpm;
            }
        }
    }
    #endregion
    public partial class MusicPlayer : MonoBehaviour
    {
        static MusicWrapper loopedMusic;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ReinitializeActiveTrack()
        {
            currentlyPlaying = new();
            Playlist = new();
        }
        public struct activeTrack
        {
            public int track;
            public MusicWrapper music;
        }
        public static activeTrack currentlyPlaying { get; private set; }
        public static bool IsPlayingOnTrack(int track, MusicWrapper music)
        {
            if (currentlyPlaying.music != music)
            {
                return false;
            }
            return currentlyPlaying.track == track;
        }
        public static float GlobalVolume { get; private set; }
        [SerializeField] MusicWrapper testStartingMusic;
        static Queue<MusicWrapper> Playlist;
        [SerializeField] List<MusicWrapper> testPlaylist = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClearPlaylist()
        {
            if (Playlist == null)
            {
                Playlist = new Queue<MusicWrapper>();
            }
            Playlist.Clear();
        }
        public static void AddToPlaylist(MusicWrapper w)
        {
            Playlist.Enqueue(w);
        }
        private void Start()
        {
            if (testStartingMusic != null)
            {
                PlayMusicWrapper(testStartingMusic);
            }
            foreach (var item in testPlaylist)
            {
                if (item == null)
                    continue;
                Playlist.Enqueue(item);
            }
        }
        private void Update()
        {
            RunBeat();
            if (!Application.isFocused || IsPlaying || isFading)
                return;

            if (Playlist.Count <= 0)
                return;

            MusicWrapper wrapper = Playlist.Dequeue();
            wrapper.Play();
        }
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            GlobalVolume = 0.75f;
            if (track1 == null) track1 = new GameObject("Music Track 1").transform.SetParentDecorator(transform).gameObject.AddComponent<AudioSource>();
            if (track2 == null) track2 = new GameObject("Music Track 2").transform.SetParentDecorator(transform).gameObject.AddComponent<AudioSource>();
            transform.SetParent(null);
            instance = this;
            DontDestroyOnLoad(transform.gameObject);
            SetPlayMode(FetchPlaymode());
        }
        static MusicPlayer instance;
        [SerializeField] AudioSource track1;
        [SerializeField] AudioSource track2;
        MusicWrapper song1;
        MusicWrapper song2;
        [SerializeField] float crossFadeLength = 1f;
        int selectedTrack = 0;
        public static bool IsPlaying => instance.track1.isPlaying || instance.track2.isPlaying;
        public static void PlayMusicWrapper(MusicWrapper mw)
        {
            if (mw == null)
            {
                Debug.Log("Music Wrapper is null");
                return;
            }

            if (instance == null || instance.isFading)
            {
                AddToPlaylist(mw);
                return;
            }

            if (mw.dontReplaceSelf && IsPlayingOnTrack(instance.selectedTrack, mw))
                return;

            MusicPopup.QueuePopup(mw.TrackName);
            instance.PlayCrossfade(mw, instance.crossFadeLength);
            if (currentPlayMode == PlayMode.Loop)
            {
                loopedMusic = mw;
            }

            double dsp = AudioSettings.dspTime;
            nextBeatDspTime = dsp + mw.BeatLength;
        }
        private void PlayCrossfade(MusicWrapper clip, float crossfade = 0.5f)
        {
            StartCoroutine(FadeTrack(clip, (!track1.isPlaying && !track2.isPlaying ? 0.5f : 0), crossfade));
        }
        public static void CurrentTrackSetTime(float time)
        {
            if (instance == null)
            {
                return;
            }
            AudioSource track = instance.selectedTrack == 1 ? instance.track1 : instance.track2;
            if (track == null || track.clip == null)
            {
                return;
            }
            float thresholdSeconds = 0.01f;
            int thresholdSamples = Mathf.FloorToInt(thresholdSeconds * track.clip.frequency);
            int desiredSample = Mathf.FloorToInt(time * track.clip.frequency);
            track.Pause();
            if (Mathf.Abs(track.timeSamples - desiredSample) > thresholdSamples)
            {
                track.timeSamples = desiredSample;
            }
            track.Play();
        }
        public static WaitUntil FadeOutAndWait()
        {
            if (instance == null)
                return null;
            if (IsPlaying)
            {
                AudioSource s = instance.selectedTrack == 1 ? instance.track1 : instance.track2;
                instance.StartCoroutine(instance.FadeOut(s, instance.crossFadeLength));
            }
            return WaitForNoMusic;
        }
        private IEnumerator FadeOut(AudioSource s, float crossfade)
        {
            crossfade = crossfade.Max(0.00f);
            float timeElapsed = 0f;
            if (crossfade == 0)
            {
                s.volume = 0f;
            }
            else
            {
                while (timeElapsed < crossfade)
                {
                    s.volume = Mathf.Lerp(song1 * GlobalVolume, 0f, timeElapsed / crossfade);
                    timeElapsed += Time.deltaTime;
                    yield return null;
                }
            }
            s.Stop();
        }
        public static WaitUntil WaitForNoMusic => new WaitUntil(() => !IsPlaying);
        private IEnumerator FadeTrack(MusicWrapper newClip, float delay, float fadeDuration)
        {
            yield return delay.WaitForSeconds(false);
            if (isFading) yield break;
            isFading = true;

            activeTrack newTrack = new();
            fadeDuration = Mathf.Max(0f, fadeDuration);

            if (newClip.musicClip == null)
            {
                Debug.LogWarning("Missing Audio Clip in MusicWrapper : " + newClip.name);
                isFading = false;
                yield break;
            }

            AudioSource fromSource = selectedTrack == 2 ? track2 : track1;
            MusicWrapper fromSong = selectedTrack == 2 ? song2 : song1;

            AudioSource toSource = selectedTrack == 2 ? track1 : track2;
            selectedTrack = selectedTrack == 2 ? 1 : 2;

            float fromStartVol = fromSong != null ? fromSong.musicVolume * GlobalVolume : 0f;

            if (fromSource.isPlaying && fromSong != null && fadeDuration > 0f)
            {
                float time = 0f;
                while (time < fadeDuration)
                {
                    float t = time / fadeDuration;
                    fromSource.volume = Mathf.Lerp(fromStartVol, 0f, t);
                    time += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (fromSource.isPlaying)
            {
                fromSource.Stop();
                fromSource.volume = 0f;
            }
            toSource.clip = newClip.musicClip;
            toSource.volume = 0f;
            toSource.Play();

            toSource.loop = currentPlayMode != PlayMode.Shuffle;

            float toTargetVol = newClip.musicVolume * GlobalVolume;

            toSource.volume = toTargetVol;
            QueueShuffleTrack();
            if (selectedTrack == 1) song1 = newClip;
            else song2 = newClip;

            newTrack.track = selectedTrack;
            newTrack.music = newClip;
            currentlyPlaying = newTrack;

            isFading = false;
        }

    }
}