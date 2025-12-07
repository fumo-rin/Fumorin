using FumoCore.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fumorin
{
    [RequireComponent(typeof(TMP_Text))]
    public class RebindTextDisplay : MonoBehaviour
    {
        [SerializeField] string replaceWithKeybind = "##";
        [SerializeField] string text = "";
        [SerializeField] InputActionReference keybind;
        TMP_Text t;
        private void Awake()
        {
            t = GetComponent<TMP_Text>();
        }
        private void Start()
        {
            if (keybind == null)
            {
                Debug.LogWarning($"Missing Keybind for {transform.name} in {nameof(RebindTextDisplay)}");
                return;
            }
            if (!RebindHandler.TryGetReadableBindings(keybind, out string s))
            {
                Debug.LogWarning("Missing Keybind when built Rebind text!");
            }
            t.text = text.ReplaceWordsSpaced(replaceWithKeybind, s);
        }
    }
}
