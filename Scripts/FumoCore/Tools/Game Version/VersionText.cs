using TMPro;
using UnityEngine;

namespace RinCore
{
    [RequireComponent(typeof(TMP_Text))]
    public class VersionText : MonoBehaviour
    {
        TMP_Text text;
        [SerializeField] TMP_Text versionOnlyText;
        [SerializeField] bool dontShowVersion = false;
        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            text.text = "";
            VersionManager.GameVersion v = VersionManager.GetCurrentVersion();
            if (versionOnlyText == null)
            {
                text.text = v.name;
                if (!dontShowVersion) text.text += " v" + v.version;
            }
            else
            {
                text.text = v.name;
                if (versionOnlyText != null) versionOnlyText.text = "v" + v.version;
            }
        }
    }
}
