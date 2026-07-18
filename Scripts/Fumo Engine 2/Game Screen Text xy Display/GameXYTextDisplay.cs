using System.Collections;
using TMPro;
using UnityEngine;

namespace rinCore
{
    public class GameXYTextDisplay : MonoBehaviour
    {
        #region Text Packet
        [System.Serializable]
        public class textPacket
        {
            public float fadeIn = 0.5f;
            public float fadeOut = 0.5f;
            public float duration;
            public Color32 color;

            public Vector2 a01;
            public Vector2 b01;

            public float fontSize;

            public HorizontalAlignmentOptions horizontalAlignment;
            public VerticalAlignmentOptions verticalAlignment;
            public textPacket()
            {
                this.fadeIn = 0.5f;
                this.fadeOut = 0.5f;
            }
        }
        #endregion

        [SerializeField] TMP_Text cloneable;
        [SerializeField] RectTransform textSpaceAnchor;

        static GameXYTextDisplay instance;

        private void Awake()
        {
            instance = this;
            cloneable.gameObject.SetActive(false);
        }

        public static void CreateText(string text, textPacket packet)
        {
            if (!RinHelper.ValidGameObjects(instance))
                return;

            instance.StartCoroutine(CO_Text(text, packet));

            IEnumerator CO_Text(string text, textPacket packet)
            {
                TMP_Text clone = Instantiate(instance.cloneable, instance.textSpaceAnchor);
                RectTransform rt = clone.rectTransform;
                Rect parentRect = instance.textSpaceAnchor.rect;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                Vector2 min = Vector2.Min(packet.a01, packet.b01);
                Vector2 max = Vector2.Max(packet.a01, packet.b01);

                Vector2 center = (min + max) * 0.5f;
                Vector2 size = max - min;

                rt.anchoredPosition = new Vector2(
                    (center.x - 0.5f) * parentRect.width,
                    (center.y - 0.5f) * parentRect.height);

                rt.sizeDelta = new Vector2(
                    size.x * parentRect.width,
                    size.y * parentRect.height);

                clone.enableAutoSizing = true;
                clone.fontSizeMax = packet.fontSize;
                clone.fontSizeMin = packet.fontSize * 0.25f;

                clone.horizontalAlignment = packet.horizontalAlignment;
                clone.verticalAlignment = packet.verticalAlignment;

                clone.text = text;
                clone.color = packet.color.Opacity(0);
                clone.gameObject.SetActive(true);

                float entry = packet.fadeIn;
                while (entry > 0)
                {
                    float lerp01 = entry.MapTo01(packet.fadeIn, 0f, true);

                    clone.color = clone.color.Opacity(
                        lerp01.MapFrom01(0f, 255f).ToByte());

                    entry -= Time.deltaTime;
                    yield return null;
                }

                clone.color = packet.color;

                yield return packet.duration.WaitForSeconds();

                float exit = packet.fadeOut;
                while (exit > 0)
                {
                    float lerp01 = exit.MapTo01(packet.fadeOut, 0f, true);

                    clone.color = clone.color.Opacity(
                        lerp01.MapFrom01(255f, 0f).ToByte());

                    exit -= Time.deltaTime;
                    yield return null;
                }

                Destroy(clone.gameObject);
            }
        }
    }
}