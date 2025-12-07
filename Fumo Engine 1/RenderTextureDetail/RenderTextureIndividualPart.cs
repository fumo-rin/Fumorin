using UnityEngine;

namespace Fumorin
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
        public static void SetNewSize(int x, int y)
        {
            screenSize = new(x, y);
            WhenChangeSize?.Invoke(x, y);
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
