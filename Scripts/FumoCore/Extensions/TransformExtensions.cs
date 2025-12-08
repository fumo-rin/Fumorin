using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RinCore
{
    public static class GameObjectExtensions
    {
        public static void MoveToSceneOf(this GameObject gameObject, GameObject targetObject)
        {
            if (gameObject == null)
            {
                return;
            }
            if (targetObject == null)
            {
                return;
            }
            Scene targetScene = targetObject.scene;
            if (gameObject.scene == targetScene)
            {
                return;
            }
            SceneManager.MoveGameObjectToScene(gameObject, targetScene);
        }
    }
    public static partial class TransformExtensions
    {
        /*public static Transform Rotate(this Transform t, float x, float y)
        {
            if (x == 0 && y == 0)
                return t;
            Quaternion r = t.rotation;
            r *= Quaternion.Euler(y, x, 0);
            t.rotation = r;
            return t;
        }*/
        public static Transform SetParentDecorator(this Transform t, Transform newParent)
        {
            t.SetParent(newParent);
            return t;
        }
        public static Vector3 GetVectorTowards(this Transform t, Vector3 target)
        {
            return target - t.position;
        }
        public static T FindInRootAndChildren<T>(this Transform t)
        {
            if (t.GetComponent<T>() is T foundOnTransform)
            {
                return foundOnTransform;
            }
            if (t.root.GetComponent<T>() is T foundRoot)
            {
                return foundRoot;
            }
            if (t.root.GetComponentInChildren<T>() is T foundInChildren)
            {
                return foundInChildren;
            }
            Debug.LogWarning($"Failed to find Component on {t.name} of type : " + typeof(T).ToString());
            return default(T);
        }
        public static T FindBelow<T>(this Transform t)
        {
            if (t.GetComponent<T>() is T foundOnTransform)
            {
                return foundOnTransform;
            }
            if (t.root.GetComponentInChildren<T>() is T foundInChildren)
            {
                return foundInChildren;
            }
            Debug.LogWarning($"Failed to find Component on {t.name} of type : " + typeof(T).ToString());
            return default(T);
        }
        public static T FindInRootAndChildren<T>(this GameObject g)
        {
            return g.transform.FindInRootAndChildren<T>();
        }
        public static bool TryLayoutRebuild(this Transform t)
        {
            if (t.GetComponent<RectTransform>() is RectTransform rect)
            {
                LayoutRebuilder.MarkLayoutForRebuild(rect);
                return true;
            }
            return false;
        }
        public enum LookDirection2D
        {
            Right,
            Left,
            Up,
            Down
        }
        public static void Lookat2D(this Transform t, Vector2 position, LookDirection2D cardinal = LookDirection2D.Right)
        {
            Vector2 direction = position - (Vector2)t.position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            switch (cardinal)
            {
                case LookDirection2D.Right:
                    break;
                case LookDirection2D.Left:
                    angle += 180f;
                    break;
                case LookDirection2D.Up:
                    angle -= 90f;
                    break;
                case LookDirection2D.Down:
                    angle += 90f;
                    break;
            }
            if (float.IsNaN(angle) || float.IsInfinity(angle))
            {
                return;
            }
            t.rotation = Quaternion.Normalize(Quaternion.Euler(0f, 0f, angle));
        }
        public static void Lookat2DLerp(this Transform t, Vector2 direction, float delta, float angleOffset = 0f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.AngleAxis(angle, Vector3.forward), delta *
            Time.deltaTime);
        }
        public static void ScaleToSpriteHeight(this Transform transform, SpriteRenderer spriteRenderer, float desiredHeight)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }
            Sprite sprite = spriteRenderer.sprite;

            float spritePixelHeight = sprite.rect.height;
            float spritePixelWidth = sprite.rect.width;
            float ppu = sprite.pixelsPerUnit;

            float spriteUnitHeight = spritePixelHeight / ppu;
            float spriteUnitWidth = spritePixelWidth / ppu;

            Vector3 currentScale = transform.localScale;

            float scaleFactor = desiredHeight / spriteUnitHeight;

            transform.localScale = new Vector3(
                scaleFactor * (currentScale.x / currentScale.y),
                scaleFactor,
                currentScale.z
            );
        }
    }
}