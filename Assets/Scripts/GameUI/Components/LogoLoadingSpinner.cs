using UnityEngine;
using UnityEngine.UI;

namespace GameUI.Components {
    public class LogoLoadingSpinner : MonoBehaviour {
        [SerializeField] private Image logoRing;

        private void OnEnable() {
            logoRing.fillClockwise = true;
            logoRing.fillAmount = 0;
        }

        private void Update() {
            logoRing.fillAmount += Time.unscaledDeltaTime * (logoRing.fillClockwise ? 2f : -2f);
            if (logoRing.fillAmount >= 1) logoRing.fillClockwise = false;
            if (logoRing.fillAmount <= 0) logoRing.fillClockwise = true;
        }
    }
}