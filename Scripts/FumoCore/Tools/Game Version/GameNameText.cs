using TMPro;
using UnityEngine;

namespace RinCore
{
    [RequireComponent(typeof(TMP_Text))]
    public class GameNameText : MonoBehaviour
    {
        TMP_Text text;
        [SerializeField] bool lineBreakSpaces;
        private void Start()
        {
            text = GetComponent<TMP_Text>();
            text.text = lineBreakSpaces ? Application.productName.ReplaceLineBreaks(" ") : Application.productName;
        }
    }
}
