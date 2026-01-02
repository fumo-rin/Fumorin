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
        bool started = false; // this is for external ready checks
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
            if (track1 == null) track1 = new GameObject("Music Track 1").transform.SetParentDecorator(transform).gameObject.AddComponent<AudioSource>();
            if (track2 == null) track2 = new GameObject("Music Track 2").transform.SetParentDecorator(transform).gameObject.AddComponent<AudioSource>();
            transform.SetParent(null);
            instance = this;
            DontDestroyOnLoad(transform.gameObject);
            SetPlayMode(FetchPlaymode());
        }
        static MusicPlayer instance;
        public static bool IsReady => instance != null && instance.started;
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