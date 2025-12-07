using FumoCore.Tools;
using QFSW.QC;
using UnityEngine;

namespace Fumorin
{
    public class RenderTextureDetail : MonoBehaviour
    {

        [SerializeField] RenderTexture lowD, medD, highD;
        public delegate void TextureSet(RenderTexture texture);
        public static event TextureSet WhenSetTexture;
        private static RenderMode _lastMode = RenderMode.Lowres;
        private const string RenderModeKey = "RenderMode";
        static RenderTextureDetail instance;
        static RenderTexture _texture;
        private void Awake()
        {
            instance = this;
        }
        public enum RenderMode
        {
            Lowres = 0,
            Highres = 1,
            VeryHighres = 2
        }
        [Command("-rendermode")]
        public static void SetMode(RenderMode mode)
        {
            if (mode != _lastMode)
            {
                _lastMode = mode;
                PlayerPrefs.SetInt(RenderModeKey, (int)_lastMode);
                PlayerPrefs.Save();
                RebuildTexture();
            }
        }
        public static bool GetCurrentTexture(out RenderTexture r)
        {
            r = _texture;
            if (instance == null)
            {
                return false;
            }
            if (_texture == null)
            {
                if (PlayerPrefs.HasKey(RenderModeKey))
                    _lastMode = (RenderMode)PlayerPrefs.GetInt(RenderModeKey);
                else
                    _lastMode = RenderMode.Highres;
                RebuildTexture();
                r = _texture;
            }
            return _texture != null;
        }
        private static void RebuildTexture()
        {
            _texture = BuildTextureForMode(_lastMode);
            WhenSetTexture?.Invoke(_texture);
        }
        private static RenderTexture BuildTextureForMode(RenderMode mode)
        {
            switch (mode)
            {
                case RenderMode.Lowres:
                    return instance.lowD;
                case RenderMode.Highres:
                    return instance.medD;
                case RenderMode.VeryHighres:
                    return instance.highD;
            };
            return instance.lowD;
        }
    }
}