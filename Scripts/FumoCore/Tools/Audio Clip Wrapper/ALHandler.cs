using UnityEngine;

namespace RinCore
{
    public class ALHandler : MonoBehaviour
    {
        static ALHandler instance;
        static Transform trackedTarget;
        public static Vector2 Position => trackedTarget != null ? trackedTarget.position : instance != null ? instance.transform.position : Vector2.zero;
        [Initialize(-9000)]
        private static void RestartALHandler()
        {
            instance = null;
            trackedTarget = null;
        }
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                if (gameObject.GetComponent<AudioListener>())
                {
                    return;
                }
                gameObject.AddComponent<AudioListener>();
            }
        }
        public static ALHandler CreateOrUpdate(Transform t)
        {
            trackedTarget = t;
            if (instance == null)
            {
                GameObject g = new GameObject("Audio Listener Handler");
                ALHandler a = g.AddComponent<ALHandler>();
                a.enabled = true;
                instance = a;
                DontDestroyOnLoad(a.gameObject);
                return a;
            }
            return instance;
        }
        private void Update()
        {
            if (instance != null && trackedTarget != null)
            {
                instance.transform.position = trackedTarget.position + new Vector3(0f, 0f, 0f);
            }
            else
            {
                if (instance != null && Camera.main != null)
                {
                    instance.transform.position = Camera.main.transform.position;
                }
            }
        }
    }
}
