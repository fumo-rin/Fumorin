using UnityEngine;

namespace RinCore
{
    public class RenderTextureIndividualPart : MonoBehaviour
    {
        [SerializeField] RenderTexture t;
        delegate void ScreenSizeChange(int x, int y);
        private static event ScreenSizeChange WhenChangeSize;
        static (int, int) screenSize = (480, 640);
        private void Start()
        {
            WhenChangeSize += SetLocalSize;
            if (screenSize.Item1 > 0 && screenSize.Item2 > 0)
            {
                SetLocalSize(screenSize.Item1, screenSize.Item2);
            }
        }
        private void OnDestroy()
        {
            WhenChangeSize -= SetLocalSize;
        }
        [QFSW.QC.Command("render-size")]
        public static void SetNewSize(int x, int y, (int, int)? clampMax = null)
        {
            screenSize = new(x.Clamp(180, clampMax != null ? clampMax.Value.Item1 : 960), y.Clamp(240, clampMax != null ? clampMax.Value.Item2 : 1280));
            WhenChangeSize?.Invoke(screenSize.Item1, screenSize.Item2);
        }
        private void SetLocalSize(int x, int y)
        {
            t.Release();
            t.width = x;
            t.height = y;
            t.Create();
        }
    }
}
