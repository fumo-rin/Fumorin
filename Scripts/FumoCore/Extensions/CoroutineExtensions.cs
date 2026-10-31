using System.Collections;
using UnityEngine;

namespace rinCore
{
    public static class CoroutineExtensions
    {
        public static IEnumerator Wrap(this IEnumerator routine, System.Action onComplete)
        {
            if (routine != null)
            {
                while (routine.MoveNext())
                {
                    yield return routine.Current;
                }
            }

            onComplete?.Invoke();
        }
        public static IEnumerator Wrap(this IEnumerator routine, IEnumerator b)
        {
            if (routine != null)
            {
                while (routine.MoveNext())
                    yield return routine.Current;
            }
            if (b != null)
            {
                while (b.MoveNext())
                    yield return b.Current;
            }
        }
    }
}
