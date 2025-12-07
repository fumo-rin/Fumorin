using FumoCore.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace Fumorin
{
    [CreateAssetMenu(menuName = "Bremsengine/Music Room Tracklist")]
    public class MusicRoomTracklist : ScriptableObject
    {
        public List<MusicWrapper> TrackList = new();
        public bool QueueRandomTrack(in Queue<MusicWrapper> Playlist)
        {
            Debug.Log("T");
            if (TrackList == null)
            {
                return false;
            }
            Debug.Log("T");
            MusicWrapper CurrentlyPlaying = MusicPlayer.currentlyPlaying.music;
            int attempts = 20;
            while (attempts > 0)
            {
                Debug.Log("Loop : " + (20 - attempts));
                attempts--;
                MusicWrapper piece = TrackList[0.RandomBetween(0, TrackList.Count) % TrackList.Count];
                Debug.Log(piece.TrackName);
                if (piece != null && piece != CurrentlyPlaying && !Playlist.Contains(piece))
                {
                    Debug.Log("Piece = " + piece.TrackName);
                    Playlist.Enqueue(piece);
                    return true;
                }
            }
            return false;
        }
    }
}
