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

        [SerializeField] private Text topHeader;
        [SerializeField] private Text topSubHeader;
        [SerializeField] private Text topLeftHeader;
        [SerializeField] private Text topLeftContent;
        [SerializeField] private Text topRightHeader;
        [SerializeField] private Text topRightContent;
        [SerializeField] private Text centralHeader;
        [SerializeField] private Text centralContent;
        [SerializeField] private Text centralNotification;

        private CanvasGroup _canvasGroup;

        public CanvasGroup TopCanvasGroup => topCanvasGroup;
        public CanvasGroup LeftCanvasGroup => leftCanvasGroup;
        public CanvasGroup RightCanvasGroup => rightCanvasGroup;
        public CanvasGroup CentralCanvasGroup => centralCanvasGroup;
        public CanvasGroup CentralNotificationCanvasGroup => centralNotificationCanvasGroup;

        public Text TopHeader => topHeader;
        public Text TopSubHeader => topSubHeader;
        public Text TopLeftHeader => topLeftHeader;
        public Text TopLeftContent => topLeftContent;
        public Text TopRightHeader => topRightHeader;
        public Text TopRightContent => topRightContent;
        public Text CentralHeader => centralHeader;
        public Text CentralContent => centralContent;
        public Text CentralNotification => centralNotification;

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