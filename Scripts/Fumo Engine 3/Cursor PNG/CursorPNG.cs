using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Search;

namespace rinCore
{
    public class CursorPNG : MonoBehaviour
    {
        public static int SizeModifier = 1;

        [System.Serializable]
        public struct Entry
        {
            [SearchContext("label:SaneCursors")] public Sprite sprite;
            public Vector2? hotspotOverride;
            public int priority;

            public Entry(string name, Sprite s, int priority, Vector2? hotspotOverride = null)
            {
                sprite = s;
                this.priority = priority;
                this.hotspotOverride = hotspotOverride;
            }
        }

        [SerializeField] Entry defaultCursor;
        private List<Entry> frameEntries = new();
        private Dictionary<(Sprite sprite, int scale), Texture2D> convertedTextureCache = new();

        private Sprite lastAppliedSprite;
        private int lastAppliedScale = -1;

        public record Cursor_Set_Frame(Entry entry);

        private void OnEnable()
        {
            frameEntries.Clear();
            EventBus.Bind<Cursor_Set_Frame>(OnCursorSetFrame);
            EventBus.Bind<Cursor_Set_Size>(SetSize);
            if (PersistentJSON.TryLoad(out int size, settingName))
            {
                new Cursor_Set_Size(size).Publish();
            }
        }

        private void OnDisable()
        {
            EventBus.Release<Cursor_Set_Frame>(OnCursorSetFrame);
            EventBus.Release<Cursor_Set_Size>(SetSize);
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            lastAppliedSprite = null;
            lastAppliedScale = -1;
        }

        public record Cursor_Set_Size(int size);
        const string settingName = "Setting_Cursor_Size";
        [QFSW.QC.Command("-cursor-size")]
        private void SetSize(int newSize)
        {
            newSize = newSize.Clamp(0, 8);
            SizeModifier = newSize;
            PersistentJSON.TrySave(newSize, settingName);
        }
        private void SetSize(Cursor_Set_Size newSize)
        {
            SetSize(newSize.size);
        }
        private void OnCursorSetFrame(Cursor_Set_Frame action)
        {
            frameEntries.Add(action.entry);
        }

        private void LateUpdate()
        {
            if (!Application.isFocused || IsPointerOffScreen())
            {
                frameEntries.Clear();
                return;
            }

            Entry highestEntry = defaultCursor;

            for (int i = 0; i < frameEntries.Count; i++)
            {
                if (frameEntries[i].priority > highestEntry.priority)
                {
                    highestEntry = frameEntries[i];
                }
            }

            ApplyNativeCursor(highestEntry);
            frameEntries.Clear();
        }

        private bool IsPointerOffScreen()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            Vector2 mousePos = mouse.position.ReadValue();
            return mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height;
        }

        private void ApplyNativeCursor(Entry entry)
        {
            int currentScale = Mathf.Max(0, SizeModifier);

            if (entry.sprite == null)
            {
                if (lastAppliedSprite != null || lastAppliedScale != currentScale)
                {
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    lastAppliedSprite = null;
                    lastAppliedScale = currentScale;
                }
                return;
            }

            if (entry.sprite == lastAppliedSprite && currentScale == lastAppliedScale)
            {
                return;
            }

            int textureScale = Mathf.Max(1, currentScale);
            Texture2D cursorTex = GetOrCreateCursorTexture(entry.sprite, textureScale);

            if (cursorTex != null)
            {
                Vector2 hotspot = (entry.hotspotOverride ?? GetSpriteHotspot(entry.sprite)) * textureScale;
                CursorMode mode = (currentScale == 0) ? CursorMode.Auto : CursorMode.ForceSoftware;
                Cursor.SetCursor(cursorTex, hotspot, mode);

                lastAppliedSprite = entry.sprite;
                lastAppliedScale = currentScale;
            }
        }

        private Vector2 GetSpriteHotspot(Sprite sprite)
        {
            return new Vector2(sprite.pivot.x, sprite.rect.height - sprite.pivot.y);
        }

        private Texture2D GetOrCreateCursorTexture(Sprite sprite, int scale)
        {
            var cacheKey = (sprite, scale);
            if (convertedTextureCache.TryGetValue(cacheKey, out Texture2D existingTex) && existingTex != null)
            {
                return existingTex;
            }

            Texture2D sourceTex = sprite.texture;
            Rect r = sprite.rect;

            int targetWidth = (int)r.width * scale;
            int targetHeight = (int)r.height * scale;

            RenderTexture rt = RenderTexture.GetTemporary(
                targetWidth,
                targetHeight,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );

            Vector2 uvMin = new Vector2(r.x / sourceTex.width, r.y / sourceTex.height);
            Vector2 uvMax = new Vector2((r.x + r.width) / sourceTex.width, (r.x + r.height) / sourceTex.height);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, targetWidth, targetHeight, 0);

            Graphics.DrawTexture(
                new Rect(0, 0, targetWidth, targetHeight),
                sourceTex,
                new Rect(uvMin.x, uvMin.y, uvMax.x - uvMin.x, uvMax.y - uvMin.y),
                0, 0, 0, 0
            );

            GL.PopMatrix();

            Texture2D croppedTex = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false, false);
            croppedTex.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            croppedTex.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            croppedTex.filterMode = FilterMode.Point;
            convertedTextureCache[cacheKey] = croppedTex;
            return croppedTex;
        }

        private void OnDestroy()
        {
            foreach (var tex in convertedTextureCache.Values)
            {
                if (tex != null)
                {
                    Destroy(tex);
                }
            }
            convertedTextureCache.Clear();
        }
    }
}