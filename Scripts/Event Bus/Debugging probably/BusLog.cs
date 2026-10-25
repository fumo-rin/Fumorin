using System.Collections;
using TMPro;
using UnityEngine;

namespace rinCore
{
    public record BusLogEntry(string message, float duration = 3f) : IRinEvent
    {
        public IEnumerator CO_Run(TMP_Text text)
        {
            if (text == null || text.transform == null)
                yield break;
            TMP_Text instance = GameObject.Instantiate(text, text.transform.parent);
            instance.text = message;
            instance.gameObject.SetActive(true);
            yield return duration.WaitForSeconds();
            GameObject.Destroy(instance.gameObject);
        }
    }
    public class BusLog : MonoBehaviour
    {
        [SerializeField] TMP_Text textEntry;
        void LogItem(BusLogEntry e)
        {
            StartCoroutine(e.CO_Run(textEntry));
        }
        private void Awake()
        {
            textEntry.gameObject.SetActive(false);
        }
        private void OnEnable()
        {
            RinBus.Bind<BusLogEntry>(LogItem);
        }
        private void OnDisable()
        {
            RinBus.Release<BusLogEntry>(LogItem);
        }
    }
}
