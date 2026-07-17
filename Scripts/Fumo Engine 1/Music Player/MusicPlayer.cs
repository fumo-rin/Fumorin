using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace rinCore
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
            if (loopedMusic == null || instance == null)
            {
                return;
            }

            instance.track1.loop = true;
            instance.track2.loop = true;
            loopedMusic.Play();
        }
    }
    #endregion

    public partial class MusicPlayer : MonoBehaviour
    {
        static MusicWrapper loopedMusic;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ReinitializeActiveTrack()
        {
            instance = null;
            currentlyPlaying = new();
            Playlist = new();
            loopedMusic = null;
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
                if (item == null) continue;
                Playlist.Enqueue(item);
            }
        }

        bool started = false;

        private void Update()
        {
            if (started)
            {
                if (!Application.isFocused || IsPlaying || isFading)
                    return;
            }
            if (Playlist.Count > 0)
            {
                MusicWrapper wrapper = Playlist.Dequeue();
                wrapper.Play();
            }
            started = true;
        }

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            GlobalVolume = 0.75f;

            if (track1 == null) track1 = gameObject.AddComponent<AudioSource>();
            if (track2 == null) track2 = gameObject.AddComponent<AudioSource>();

            transform.SetParent(null);
            instance = this;
            DontDestroyOnLoad(transform.gameObject);
            SetPlayMode(FetchPlaymode());
        }

        static MusicPlayer instance;
        public static bool IsReady => instance != null && instance.started;

        [SerializeField] AudioSource track1;
        [SerializeField] AudioSource track2;

        private MusicWrapper song1;
        private MusicWrapper song2;

        [SerializeField] float crossFadeLength = 1f;
        int selectedTrack = 0; // 0 = None, 1 = track1, 2 = track2

        private Coroutine transitionCoroutine;
        private Coroutine fadeOutCoroutine;

        public static bool IsPlaying => (instance.track1 != null && instance.track1.isPlaying) || (instance.track2 != null && instance.track2.isPlaying);

        public static void PlayMusicWrapper(MusicWrapper mw)
        {
            if (mw == null)
            {
                Debug.Log("Music Wrapper is null");
                return;
            }

            if (instance == null)
            {
                AddToPlaylist(mw);
                return;
            }

            if (mw.dontReplaceSelf && IsPlayingOnTrack(instance.selectedTrack, mw))
                return;

            instance.PlayTransition(mw, instance.crossFadeLength);

            if (currentPlayMode == PlayMode.Loop)
            {
                loopedMusic = mw;
            }
        }

        private void PlayTransition(MusicWrapper clip, float fadeDuration = 0.5f)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            transitionCoroutine = StartCoroutine(TransitionSequence(clip, fadeDuration));
        }

        public static void CurrentTrackSetTime(float time)
        {
            if (instance == null) return;

            AudioSource track = instance.selectedTrack == 1 ? instance.track1 : instance.track2;
            if (track == null || track.clip == null) return;

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
            if (instance == null) return null;

            if (IsPlaying)
            {
                if (instance.transitionCoroutine != null)
                {
                    instance.StopCoroutine(instance.transitionCoroutine);
                    instance.transitionCoroutine = null;
                    instance.isFading = false;
                }

                AudioSource s = instance.selectedTrack == 1 ? instance.track1 : instance.track2;
                MusicWrapper w = instance.selectedTrack == 1 ? instance.song1 : instance.song2;

                if (instance.fadeOutCoroutine != null)
                {
                    instance.StopCoroutine(instance.fadeOutCoroutine);
                }
                instance.fadeOutCoroutine = instance.StartCoroutine(instance.FadeOut(s, w, instance.crossFadeLength));
            }
            return WaitForNoMusic;
        }

        private IEnumerator FadeOut(AudioSource s, MusicWrapper w, float crossfade)
        {
            crossfade = Mathf.Max(0.00f, crossfade);
            float timeElapsed = 0f;
            float startVol = w != null ? w.musicVolume * GlobalVolume : s.volume;

            if (crossfade == 0)
            {
                s.volume = 0f;
            }
            else
            {
                while (timeElapsed < crossfade)
                {
                    s.volume = Mathf.Lerp(startVol, 0f, timeElapsed / crossfade);
                    timeElapsed += Time.deltaTime;
                    yield return null;
                }
            }
            s.Stop();
            fadeOutCoroutine = null;
        }

        public static WaitUntil WaitForNoMusic => new WaitUntil(() => !IsPlaying);

        private IEnumerator TransitionSequence(MusicWrapper newClip, float fadeDuration)
        {
            isFading = true;

            if (newClip.musicClip == null)
            {
                Debug.LogWarning("Missing Audio Clip in MusicWrapper : " + newClip.name);
                isFading = false;
                transitionCoroutine = null;
                yield break;
            }

            AudioSource fromSource = selectedTrack == 2 ? track2 : track1;
            AudioSource toSource = selectedTrack == 2 ? track1 : track2;

            if (!track1.isPlaying && !track2.isPlaying)
            {
                track1.Stop(); track1.volume = 0f;
                track2.Stop(); track2.volume = 0f;
            }
            else
            {
                float fromStartVol = fromSource.volume;
                fadeDuration = Mathf.Max(0f, fadeDuration);

                if (fadeDuration > 0f && fromSource.isPlaying)
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

                yield return new WaitForSecondsRealtime(0.05f);
            }

            selectedTrack = selectedTrack == 2 ? 1 : 2;

            toSource.clip = newClip.musicClip;
            float toTargetVol = newClip.musicVolume * GlobalVolume;
            toSource.volume = toTargetVol;
            toSource.Play();
            toSource.loop = currentPlayMode != PlayMode.Shuffle;

            if (selectedTrack == 1) song1 = newClip;
            else song2 = newClip;

            activeTrack newTrackState = new activeTrack { track = selectedTrack, music = newClip };
            currentlyPlaying = newTrackState;

            MusicPopup.QueuePopup(newClip.TrackName);

            QueueShuffleTrack();

            isFading = false;
            transitionCoroutine = null;
        }
    }
}