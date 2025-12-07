using FumoCore.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fumorin
{
    public class CreditsLoader : MonoBehaviour
    {
        [SerializeField] GameCreditsSO gameCredits;
        [SerializeField] TMP_Text creditsText;
        [SerializeField] Rigidbody2D rb;
        [SerializeField] float upwardsForce;
        [SerializeField] GameObject container;
        [SerializeField] bool LoadCreditsOnStart;
        [SerializeField] Button b;
        private void Start()
        {
            creditsText.text = gameCredits.CompileCredits();
            if (container != null) container.SetActive(LoadCreditsOnStart);
        }
        public void StartCredits()
        {
            container.SetActive(true);
            if (rb == null)
            {
                return;
            }
            rb.position = transform.position;
            rb.linearVelocity = new(0f, upwardsForce);
        }
        public void EndCredits()
        {
            container.SetActive(false);
        }
    }
}
