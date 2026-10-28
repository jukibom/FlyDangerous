using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.GameModes {
    /**
     * Collection of UI elements which all start off disabled.
     * Consumer must enable the elements as needed.
     */
    [RequireComponent(typeof(CanvasGroup))]
    public class GameModeUIText : MonoBehaviour {
        [SerializeField] private CanvasGroup topCanvasGroup;
        [SerializeField] private CanvasGroup leftCanvasGroup;
        [SerializeField] private CanvasGroup rightCanvasGroup;
        [SerializeField] private CanvasGroup centralCanvasGroup;
        [SerializeField] private CanvasGroup centralNotificationCanvasGroup;

        [SerializeField] private TMPro.TMP_Text topHeader;
        [SerializeField] private TMPro.TMP_Text topSubHeader;
        [SerializeField] private TMPro.TMP_Text topLeftHeader;
        [SerializeField] private TMPro.TMP_Text topLeftContent;
        [SerializeField] private TMPro.TMP_Text topRightHeader;
        [SerializeField] private TMPro.TMP_Text topRightContent;
        [SerializeField] private TMPro.TMP_Text centralHeader;
        [SerializeField] private TMPro.TMP_Text centralContent;
        [SerializeField] private TMPro.TMP_Text centralNotification;

        private CanvasGroup _canvasGroup;

        public CanvasGroup TopCanvasGroup => topCanvasGroup;
        public CanvasGroup LeftCanvasGroup => leftCanvasGroup;
        public CanvasGroup RightCanvasGroup => rightCanvasGroup;
        public CanvasGroup CentralCanvasGroup => centralCanvasGroup;
        public CanvasGroup CentralNotificationCanvasGroup => centralNotificationCanvasGroup;

        public TMPro.TMP_Text TopHeader => topHeader;
        public TMPro.TMP_Text TopSubHeader => topSubHeader;
        public TMPro.TMP_Text TopLeftHeader => topLeftHeader;
        public TMPro.TMP_Text TopLeftContent => topLeftContent;
        public TMPro.TMP_Text TopRightHeader => topRightHeader;
        public TMPro.TMP_Text TopRightContent => topRightContent;
        public TMPro.TMP_Text CentralHeader => centralHeader;
        public TMPro.TMP_Text CentralContent => centralContent;
        public TMPro.TMP_Text CentralNotification => centralNotification;

        private void OnEnable() {
            _canvasGroup = GetComponent<CanvasGroup>();
            HideGameUIText(false);
        }

        public void HideAll() {
            TopCanvasGroup.alpha = 0;
            LeftCanvasGroup.alpha = 0;
            RightCanvasGroup.alpha = 0;
            CentralCanvasGroup.alpha = 0;
        }

        public void ShowGameUIText(bool animate = true) {
            gameObject.SetActive(true);
            if (!animate) {
                _canvasGroup.alpha = 1;
                return;
            }

            _canvasGroup.alpha = 0;
            StartCoroutine(YieldExtensions.SimpleAnimationTween(val => _canvasGroup.alpha = val, 0.5f));
        }

        public void HideGameUIText(bool animate = true) {
            if (!animate) {
                _canvasGroup.alpha = 0;
                return;
            }

            _canvasGroup.alpha = 1;
            StartCoroutine(YieldExtensions.SimpleAnimationTween(val => _canvasGroup.alpha = 1 - val, 0.5f));
        }
    }
}