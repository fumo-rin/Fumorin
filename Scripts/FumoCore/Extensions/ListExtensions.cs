using rinCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#region Populate overloads from hell.
namespace rinCore
{
    #region Single Generic Parameter Populate
    public static partial class ListExtensions
    {
        public static TList Populate<TList, T>(this TList l, params T[] items) where TList : List<T>
        {   /*                       _oo0oo_
            *                      o8888888o
            *                      88" . "88
            *                      (| -_- |)
            *                      0\  =  /0
            *                    ___/`---'\___
            *                  .' \\|     |// '.
            *                 / \\|||  :  |||// \
            *                / _||||| -:- |||||- \
            *               |   | \\\  -  /// |   |
            *               | \_|  ''\---/''  |_/ |
            *               \  .-\__  '-'  ___/-. /
            *             ___'. .'  /--.--\  `. .'___
            *          ."" '<  `.___\_<|>_/___.' >' "".
            *         | | :  `- \`.;`\ _ /`;.`/ - ` : | |
            *         \  \ `_.   \_ __\ /__ _/   .-` /  /
            *     =====`-.____`.___ \_____/___.-`___.-'=====
            *                       `=---='
            *
            *     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            *               佛祖保佑         永無BUG
            *     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*/
            if (l == null)
                throw new System.ArgumentNullException(nameof(l),
                    "Cannot populate a null list due to explicit type requirement. C# Limitation.");

            l.Clear();
            if (items != null)
            {
                l.EnsureCapacity(items.Length);

                for (int i = 0; i < items.Length; i++)
                    l.Add(items[i]);
            }
            return l;
        }
    }
    #endregion
}
#endregion

namespace rinCore
{
    public enum ListAddMode
    {
        Default,
        NoDuplicates
    }
    #region Vector List
    public static partial class VectorListHelper
    {
        public static List<Vector2> Chain(this List<Vector2> list, Vector2 v)
        {
            list.Add(v);
            return list;
        }
        public static List<Vector2> Chain(this List<Vector2> list, float x, float y)
        {
            list.Add(new Vector2(x, y));
            return list;
        }
    }
    #endregion
    public static class DictionaryExtensions
    {
        public static IEnumerable<KeyValuePair<T, TT>> DebugPrintContents<T, TT>(this Dictionary<T, TT> items)
        {
            foreach (var item in items)
            {
                Debug.Log(item.Key.ToString() + " : " + item.Value.ToString());
                yield return item;
            }
        }
    }
    public static partial class ListExtensions
    {
        public static int EnsureCapacity<T>(this List<T> list, int capacity)
        {
            if (list == null) return 0;

            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }

            return list.Capacity;
        }
        public static bool AddIfDoesntExist<T>(this List<T> l, T other)
        {
            if (!l.Contains(other))
            {
                l.Add(other);
                return true;
            }
            return false;
        }
        public static void AddList<T>(this List<T> l, List<T> other, ListAddMode addMode = ListAddMode.NoDuplicates)
        {
            if (l.GetType() == other.GetType())
            {
                foreach (T item in other)
                {
                    if (other != null)
                    {
                        if (addMode == ListAddMode.NoDuplicates && l.Contains(item))
                            continue;

                        l.Add(item);
                    }
                }
            }
        }
        public static IEnumerable<T> FastClearWithIterator<T>(this List<T> list, int maxAttempts = 5000)
        {
            int attempts = maxAttempts;
            while (list.Count > 0 && attempts > 0)
            {
                attempts--;
                yield return RemoveAndReplaceWithLast<T>(list, 0);
            }
            if (attempts == 0 && list.Count > 0)
            {
                Debug.LogWarning("FastClearWithIterator hit maxAttempts before fully clearing the list.");
            }
        }
        public static T RemoveAndReplaceWithLast<T>(this List<T> l, int i)
        {
            if (i >= l.Count)
                return default;

            int lastIndex = l.Count - 1;
            T result = l[i];
            if (i != lastIndex)
            {
                l[i] = l[lastIndex];
            }
            l[lastIndex] = default;
            l.RemoveAt(lastIndex);
            return result;
        }
        public static void ClearNull<T>(this List<T> l)
        {
            for (int i = 0; i < l.Count; i++)
            {
                if (l[i] == null)
                {
                    l.RemoveAt(i);
                    i--;
                }
            }
        }
        public static T RandomResult<T>(this List<T> l)
        {
            if (l == null || l.Count == 0)
            {
                return default(T);
            }
            T result = l[Random.Range(0, l.Count) % l.Count];
            return result;
        }
        public static bool TryGetIndex<T>(this List<T> l, int index, out T result)
        {
            if (l != null && index >= 0 && index < l.Count)
            {
                result = l[index];
                return true;
            }
            result = default;
            return false;
        }
        public static void WrapIndex<T>(this List<T> l, int value, out int result)
        {
            result = 0;
            if (l == null || l.Count <= 0) return;

            int count = l.Count;
            if (count < 2) return;

            result = value % count;
            if (result < 0)
                result += count;
        }
        public static IEnumerable<T> OrderByRandom<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(_ => RNG.Byte255);
        }
    }
}