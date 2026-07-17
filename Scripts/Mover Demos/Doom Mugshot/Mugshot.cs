using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    public class Mugshot : MonoBehaviour
    {
        public const int PRIORITY_IDLE = 5, PRIORITY_MOVEMENT = 3, PRIORITY_EXCITED = 15;
        [System.Serializable]
        public struct spriteTing
        {
            public Sprite Idle, Excited, LookLeft, LookRight;
        }
        public enum Mood
        {
            Idle,
            Excited,
            LookLeft,
            LookRight
        }
        public class MoodEntry
        {
            public Mood mood;
            public int priority;
            public float remainingDuration;
            public MoodEntry(float duration)
            {
                priority = 0;
                mood = Mood.Idle;
                remainingDuration = duration;
            }
        }
        private void Update()
        {
            MoodLoop();
        }
        private void LateUpdate()
        {
            Vector3 velocity = IVelocity.PlayerPortraitXY;
            if (velocity.x.Absolute() > 0.15f)
            {
                SetMood(velocity.x.SignInt() == 1 ? new MoodEntry(0.25f)
                {
                    priority = PRIORITY_MOVEMENT,
                    mood = Mood.LookRight
                } : new MoodEntry(0.25f)
                {
                    priority = PRIORITY_MOVEMENT,
                    mood = Mood.LookLeft
                });
                return;
            }
            SetMood(new MoodEntry(0.03f)
            {
                priority = PRIORITY_IDLE,
                mood = Mood.Idle
            });
        }
        void MoodLoop()
        {
            if (moodEntries == null)
            {
                moodEntries = new();
            }
            moodEntries.RemoveAll(x => x == null || x.remainingDuration <= 0);
            foreach (var entry in moodEntries.OrderBy(x => x.priority))
            {
                entry.remainingDuration -= Time.deltaTime;
                if (entry.remainingDuration < 0)
                {
                    continue;
                }
            }
            MoodEntry determinedItem = (moodEntries.Count > 0 && moodEntries[0] is MoodEntry e ? e : new(0.05f) { mood = Mood.Idle, priority = 50 });
            {
                switch (determinedItem.mood)
                {
                    case Mood.Idle:
                        PlayerFaceDrawer.sprite = playerImagesUI.Idle;
                        break;
                    case Mood.Excited:
                        PlayerFaceDrawer.sprite = playerImagesUI.Excited;
                        break;
                    case Mood.LookLeft:
                        PlayerFaceDrawer.sprite = playerImagesUI.LookLeft;
                        break;
                    case Mood.LookRight:
                        PlayerFaceDrawer.sprite = playerImagesUI.LookRight;
                        break;
                    default:
                        break;
                }
            }
        }
        public spriteTing playerImagesUI;
        [SerializeField] Image PlayerFaceDrawer;
        static List<MoodEntry> moodEntries;
        public static void SetMood(MoodEntry m)
        {
            if (moodEntries == null)
            {
                moodEntries = new();
            }
            moodEntries.Add(m);
        }
    }
}
