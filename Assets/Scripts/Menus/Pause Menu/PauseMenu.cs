using System.Collections;
using Audio;
using Menus.Options;
using UnityEngine;
using UnityEngine.EventSystems;
using Core;
using Core.Player;
using UnityEngine.UI;

namespace Menus.Pause_Menu {
    public enum PauseMenuState {
        Unpaused,
        PausedMainMenu,
        PausedOptionsMenu,
    }
    
    public class PauseMenu : MonoBehaviour, IPointerMoveHandler {

        [Tooltip("Used to animate the main panel")] [SerializeField]
        private GameObject mainCanvas;
        
        [Tooltip("Used to show a background (if not VR)")] [SerializeField]
        private GameObject backgroundCanvas;

        [SerializeField] private PauseMainMenu mainPanel;
        [SerializeField] private OptionsMenu optionsPanel;
        [SerializeField] private Text copyConfirmationText;
        [SerializeField] private Text seedText;
        [SerializeField] private User user;
        [SerializeField] private CursorIcon cursor;

        private PauseMenuState _menuState = PauseMenuState.Unpaused;

        public bool IsPaused => _menuState != PauseMenuState.Unpaused;
        public PauseMenuState MenuState {
            get => _menuState;
            private set {
                _menuState = value;
                UpdatePauseGameState();
            }
        }
        private Animator _panelAnimator;
        private RectTransform _rectTransform;
        private static readonly int open = Animator.StringToHash("Open");

        private void Start() {
            seedText.text = Game.Instance.IsTerrainMap ? "SEED: " + Game.Instance.Seed : "";
            _panelAnimator = mainCanvas.GetComponent<Animator>();
            _rectTransform = GetComponent<RectTransform>();
            
            // basic non-paused without enabling input or playing sounds etc
            Game.Instance.LockCursor();
            backgroundCanvas.SetActive(false);
            user.DisableUIInput();
        }
        
        public void OnGameMenuToggle() {
            switch (MenuState) {
                case PauseMenuState.Unpaused:
                    Pause();
                    break;
                case PauseMenuState.PausedMainMenu:
                    Resume();
                    break;
                case PauseMenuState.PausedOptionsMenu:
                    optionsPanel.Cancel();
                    break;
            }
        }

        public void OnPointerMove(PointerEventData eventData) {
            if (
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform, 
                    eventData.position, 
                    eventData.enterEventCamera, 
                    out var canvasPosition)
                ) {
                cursor.OnPointerMove(canvasPosition);
            }
        }

        public void Pause() {
            AudioManager.Instance.Play("ui-dialog-open");
            MenuState = PauseMenuState.PausedMainMenu;
            mainPanel.HighlightResume();
            _panelAnimator.SetBool(open, true);
        }

        public void Resume() {
            AudioManager.Instance.Play("ui-cancel");
            MenuState = PauseMenuState.Unpaused;
            _panelAnimator.SetBool(open, false);
        }

        public void Restart() {
            AudioManager.Instance.Play("ui-confirm");
            Resume();
            Game.Instance.RestartLevel();
        }

        public void OpenOptionsPanel() {
            AudioManager.Instance.Play("ui-dialog-open");
            MenuState = PauseMenuState.PausedOptionsMenu;
        }

        public void CloseOptionsPanel() {
            MenuState = PauseMenuState.PausedMainMenu;
            mainPanel.HighlightOptions();
        }

        public void Quit() {
            AudioManager.Instance.Play("ui-confirm");
            Resume();
            Game.Instance.QuitToMenu();
        }

        public void CopyLocationToClipboard() {
            AudioManager.Instance.Play("ui-confirm");
            GUIUtility.systemCopyBuffer = Game.Instance.LevelDataAtCurrentPosition.ToJsonString();
            var copyConfirmTransform = copyConfirmationText.transform;
            copyConfirmTransform.localPosition = new Vector3(copyConfirmTransform.localPosition.x, 55, copyConfirmTransform.position.z);
            copyConfirmationText.color = new Color(1f, 1f, 1f, 1f);
            
            IEnumerator FadeText() {
                while (copyConfirmationText.color.a > 0.0f) {
                    copyConfirmationText.color = new Color(1f, 1f, 1f, copyConfirmationText.color.a - Time.unscaledDeltaTime);

                    var localPosition = gameObject.transform.localPosition;
                    copyConfirmTransform.localPosition = new Vector3(
                        localPosition.x + 160, 
                        copyConfirmationText.gameObject.transform.localPosition.y + (Time.unscaledDeltaTime * 20), 
                        localPosition.z
                    );
                    yield return null;
                }
            }
            
            StartCoroutine(FadeText());
        }
        
        // toggle ship controller input and timescales
        private void UpdatePauseGameState() {
            switch (MenuState) {
                case PauseMenuState.Unpaused:
                    Game.Instance.LockCursor();
                    backgroundCanvas.SetActive(false);
                    user.EnableGameInput();
                    user.DisableUIInput();
                    user.ResetMouseToCentre();
                    Time.timeScale = 1;
                    break;
                case PauseMenuState.PausedMainMenu: 
                    Game.Instance.FreeCursor();
                    cursor.OnPointerMove(Vector2.zero);
                    backgroundCanvas.SetActive(true);
                    user.DisableGameInput();
                    user.EnableUIInput();
                    user.ResetMouseToCentre();
                    optionsPanel.Hide();
                    mainPanel.Show();
                    copyConfirmationText.color = new Color(1f, 1f, 1f, 0f);
                    Time.timeScale = 0;
                    break;
                case PauseMenuState.PausedOptionsMenu:
                    optionsPanel.Show();
                    mainPanel.Hide();
                    break;
            }
        }
    }
}