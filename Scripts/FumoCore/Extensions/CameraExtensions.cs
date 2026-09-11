using UnityEngine;
using UnityEngine.InputSystem;

namespace rinCore
{
    public readonly struct MouseWorldspaceResult
    {
        public readonly Vector2 WorldPos2D;
        public readonly Vector2Int ClampedIntegerWorldspace;
        public readonly Vector2 RectSpace01;
        public readonly Ray Ray3D;
        public readonly RaycastHit2D Hit2D;
        public readonly RaycastHit? Hit3D;

        public MouseWorldspaceResult(
            Vector2 worldPos2D,
            Vector2Int clampedIntegerWorldspace,
            Vector2 rectSpace01,
            Ray ray3D,
            RaycastHit2D hit2D,
            RaycastHit? hit3D)
        {
            WorldPos2D = worldPos2D;
            ClampedIntegerWorldspace = clampedIntegerWorldspace;
            RectSpace01 = rectSpace01;
            Ray3D = ray3D;
            Hit2D = hit2D;
            Hit3D = hit3D;
        }
    }

    public static class CameraMouseExtensions
    {
        public static bool CurrentMouseWorldspace(this Camera camera, out MouseWorldspaceResult result, LayerMask? mask2D = null, LayerMask? mask3D = null)
        {
            if (camera == null || Mouse.current == null)
            {
                result = default;
                return false;
            }

            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();

            Vector3 viewportPos = camera.ScreenToViewportPoint(mouseScreenPos);
            Vector2 rectSpace01 = new Vector2(viewportPos.x, viewportPos.y);

            Vector3 world3D = camera.ScreenToWorldPoint(mouseScreenPos);
            Vector2 worldPos2D = new Vector2(world3D.x, world3D.y);

            Vector2Int clampedIntegerWorldspace = new Vector2Int(
                Mathf.FloorToInt(worldPos2D.x),
                Mathf.FloorToInt(worldPos2D.y)
            );

            Ray ray3D = camera.ScreenPointToRay(mouseScreenPos);

            LayerMask layerMask2D = mask2D ?? Physics2D.DefaultRaycastLayers;
            RaycastHit2D hit2D = Physics2D.Raycast(worldPos2D, Vector2.zero, 0f, layerMask2D);

            RaycastHit hit3D;
            RaycastHit? optionalHit3D = null;
            LayerMask layerMask3D = mask3D ?? Physics.DefaultRaycastLayers;

            if (Physics.Raycast(ray3D, out hit3D, camera.farClipPlane, layerMask3D))
            {
                optionalHit3D = hit3D;
            }

            result = new MouseWorldspaceResult(
                worldPos2D,
                clampedIntegerWorldspace,
                rectSpace01,
                ray3D,
                hit2D,
                optionalHit3D
            );

            return true;
        }
    }
}
