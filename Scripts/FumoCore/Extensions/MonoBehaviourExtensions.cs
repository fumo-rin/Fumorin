using System;
using System.Collections;
using UnityEngine;

namespace rinCore
{
    public static class MonoBehaviourExtensions
    {
        public class InstantiateSettings
        {
            public Transform optionalParent = null;
            public bool useParentPosition = false;
        }
        public static T Instantiate2D<T>(this T input, Vector2 pos, InstantiateSettings settings = null) where T : MonoBehaviour
        {
            T result = null;
            if (input != null)
            {
                result = MonoBehaviour.Instantiate(input, pos, Quaternion.identity);
                if (settings == null)
                {
                    return result;
                }
                if (settings.optionalParent != null && result != null)
                {
                    result.transform.parent = settings.optionalParent;
                    if (settings.useParentPosition)
                    {
                        result.transform.position = settings.optionalParent.position;
                    }
                }
            }
            return result;
        }
        public static Coroutine StartCoroutineExtras<T>(this T host, IEnumerator routine, WaitUntil wait, Action onComplete) where T : MonoBehaviour
        {
            return host.StartCoroutine(Wrap(routine, wait, onComplete));
        }
        private static IEnumerator Wrap(IEnumerator routine, WaitUntil w, Action onComplete)
        {
            if (w != null)
                yield return w;
            yield return routine;
            onComplete?.Invoke();
        }
    }
}
