using UnityEngine;

namespace GameUI {
    
    [RequireComponent(typeof(CanvasGroup))]
    public class ShipStats : MonoBehaviour {
        private CanvasGroup _canvasGroup;
        private void OnEnable() {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
        
        public void ShowTimers() {
            // TODO: shiny blendy magic yay
            _canvasGroup.alpha = 1;
        }

        public void HideTimers() {
            _canvasGroup.alpha = 0;
        }
    }
}
