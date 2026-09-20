using rinCore;
using System.Collections.Generic;
using UnityEngine;

namespace rinCore
{
    [CreateAssetMenu(menuName = "Bremsengine/Music Room Tracklist")]
    public class MusicRoomTracklist : ScriptableObject
    {
        public List<MusicWrapper> TrackList;
        public bool QueueRandomTrack(in Queue<MusicWrapper> Playlist)
        {
            if (TrackList == null)
            {
                return false;
            }
            MusicWrapper CurrentlyPlaying = MusicPlayer.currentlyPlaying.music;
            int attempts = 20;
            while (attempts > 0)
            {
                attempts--;
                MusicWrapper piece = TrackList[0.RandomBetween(0, TrackList.Count) % TrackList.Count];
                Debug.Log(piece.TrackName);
                if (piece != null && piece != CurrentlyPlaying && !Playlist.Contains(piece))
                {
                    Playlist.Enqueue(piece);
                    return true;
                }
            }
            return false;
        }
    }
}
