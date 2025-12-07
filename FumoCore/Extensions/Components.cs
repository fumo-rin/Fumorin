using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FumoCore.Tools
{
    public static class Components
    {
        public static List<T> Collect2D<T>(Vector2 position, float size, LayerMask mask)
        {
            Collider2D[] hit = Physics2D.OverlapCircleAll(position, size, mask);
            List<T> result = new List<T>();
            foreach (Collider2D collider in hit)
            {
                if (collider.GetComponent<T>() is T component && component != null)
                {
                    if (component == null)
                        continue;
                    result.Add(component);
                }
            }
            result = result.Where(x => (object)x != null).ToList();
            return result;
        }
        public static T[] Collect3D<T>(Vector3 position, float size, LayerMask mask)
        {
            throw new System.Exception("Not Yet Implemented");
        }
        public static bool TryGetComponentInParents<T>(this Transform child, out T component) where T : Component
        {
            Transform current = child;
            component = null;
            while (current != null)
            {
                component = current.GetComponent<T>();
                if (component != null)
                    return component;

                current = current.parent;
            }
            return component != null;
        }
        public static bool GetComponentInParents<T>(this GameObject obj, out T component) where T : Component
        {
            return obj.transform.TryGetComponentInParents<T>(out component);
        }
        /// <summary>
        /// Sets the result to a found component, Finding on current monobehaviour and then trying on parent after if it doesnt find it.
        /// 
        /// Returns True if Component is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <param name="Component"></param>
        /// <returns></returns>
        public static bool TryGetComponentUpwards<T>(this MonoBehaviour m, out T Component) where T : Component
        {
            Component = m.GetComponent<T>();
            if (Component == null)
            {
                Component = m.GetComponentInParent<T>();
            }
            return Component != null;
        }
        public static bool TryGetComponentDownwards<T>(this MonoBehaviour m, out T Component) where T : Component
        {
            Component = m.GetComponent<T>();
            if (Component == null)
            {
                Component = m.GetComponentInChildren<T>();
            }
            return Component != null;
        }
        public static bool TryGetComponentsInChildren<T>(this Transform t, out List<T> c)
        {
            c = new();
            foreach (var item in t.GetComponentsInChildren<T>())
            {
                if (item == null) continue;
                c.AddIfDoesntExist<T>(item);
            }
            return c != null && c.Count > 0;
        }
    }
}
