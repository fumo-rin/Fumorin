using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Fumorin
{
    #region Render Texture Click
    public partial class RenderClickHandler : IPointerDownHandler
    {
        private void ScaleRenderClickToCameraWorldPosition
            (out Vector2 worldPosition, Vector2 normalizedClick, Camera fallbackCamera)
        {
            if (!GetRenderCamera(out Camera renderCamera))
            {
                worldPosition = LastPressPosition;
                return;
            }
            Vector2 cameraSize = Vector2.zero;
            cameraSize.y = renderCamera.orthographicSize * 2f;
            cameraSize.x = cameraSize.y * renderCamera.aspect;
            worldPosition = new Vector2(normalizedClick.x * cameraSize.x, normalizedClick.y * cameraSize.y) + (Vector2)renderCamera.transform.position;
            worldPosition -= cameraSize * 0.5f;
        }
        private bool RenderTextureContainsMousePosition
            (out Vector2 normalizedPosition, PointerEventData pointer, RectTransform rendererRect)
        {
            normalizedPosition = Vector2.zero;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle
                (rendererRect, pointer.position, pointer.pressEventCamera, out var localPosition))
            {
                return false;
            }
            normalizedPosition = Rect.PointToNormalized(rendererRect.rect, localPosition);
            return true;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (RenderTextureContainsMousePosition(out Vector2 click, eventData, renderTexture))
            {
                switch (eventData.button)
                {
                    case PointerEventData.InputButton.Left:
                        ScaleRenderClickToCameraWorldPosition(out Vector2 worldPosition, click, Camera.main);
                        LastPressTime = Time.unscaledTime;
                        LastPressPosition = worldPosition;
                        SendChannelEvent(PointerEventData.InputButton.Left, worldPosition);
                        break;
                    case PointerEventData.InputButton.Right:
                        ScaleRenderClickToCameraWorldPosition(out Vector2 rightClick, click, Camera.main);
                        SendChannelEvent(PointerEventData.InputButton.Right, rightClick);
                        break;
                    case PointerEventData.InputButton.Middle:
                        ScaleRenderClickToCameraWorldPosition(out Vector2 middleclick, click, Camera.main);
                        SendChannelEvent(PointerEventData.InputButton.Middle, middleclick);
                        break;
                    default:
                        break;
                }
            }
        }
    }
    #endregion
    public partial class RenderClickHandler : MonoBehaviour
    {
        [SerializeField] RectTransform renderTexture;
        [SerializeField] Camera cameraOverride;
        static Dictionary<int, RenderClickHandler> handlerChannels = new Dictionary<int, RenderClickHandler>();
        [Range(0, 16)]
        [SerializeField] int channelIndex = 0;
        public delegate void ClickAction(int channel, PointerEventData.InputButton button, Vector2 position);
        public static event ClickAction WhenClick;
        void SendChannelEvent(PointerEventData.InputButton clickButton, Vector2 worldPosition)
        {
            WhenClick?.Invoke(channelIndex, clickButton, worldPosition);
        }
        public static void BindChannelEvent(ClickAction action)
        {
            WhenClick += action;
        }
        public static void ReleaseChannelEvent(ClickAction action)
        {
            WhenClick -= action;
        }
        public static bool GetHandler(int index, out RenderClickHandler handler) => handlerChannels.TryGetValue(index, out handler);
        public static bool GetPress(int channel)
        {
            if (GetHandler(channel, out RenderClickHandler handler))
            {
                return handler.LastPressTime == Time.unscaledTime;
            }
            return false;
        }
        private float LastPressTime;
        private static bool isPressed;
        private static Vector2 LastPressPosition = Vector2.zero;
        public static Vector2 CursorWorldPosition => LastPressPosition;
        bool GetRenderCamera(out Camera c)
        {
            c = Camera.main;
            if (cameraOverride != null)
            {
                c = cameraOverride;
            }
            return c != null;
        }
        private void Awake()
        {
            handlerChannels[channelIndex] = this;
        }
        private void OnDestroy()
        {
            handlerChannels.Remove(channelIndex);
        }
    }
}
