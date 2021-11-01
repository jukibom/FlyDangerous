using System.Collections;
using Audio;
using Menus.Options;
using UnityEngine;
using UnityEngine.EventSystems;
using Core;
using Core.Player;
using Menus.Main_Menu;
using UnityEngine.UI;

namespace Menus.Pause_Menu {
    public enum PauseMenuState {
        Unpaused,
        PausedMainMenu,
        PausedOptionsMenu,
    }
    
    public class PauseMenu : MenuBase, IPointerMoveHandler {
        
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
        private RectTransform _rectTransform;

        protected override void OnOpen() {
            _menuState = PauseMenuState.PausedMainMenu;
            mainPanel.Show();
        }

        private void Start() {
            animator = mainCanvas.GetComponent<Animator>();
            seedText.text = Game.Instance.IsTerrainMap ? "SEED: " + Game.Instance.Seed : "";
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
            Open(null);
            MenuState = PauseMenuState.PausedMainMenu;
            mainPanel.HighlightResume();
        }

        public void Resume() {
            Cancel();
            MenuState = PauseMenuState.Unpaused;
        }

        public void Restart() {
            PlayApplySound();
            Resume();
            Game.Instance.RestartSession();
        }

        public void OpenOptionsPanel() {
            MenuState = PauseMenuState.PausedOptionsMenu;
        }

        public void CloseOptionsPanel() {
            MenuState = PauseMenuState.PausedMainMenu;
            mainPanel.HighlightOptions();
        }

        public void Quit() {
            PlayApplySound();
            Resume();
            Game.Instance.LeaveSession();
        }

        public void CopyLocationToClipboard() {
            PlayApplySound();
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
                case PauseMenuState.PausedMainMenu: 
                    Game.Instance.PauseGameToggle(true);
                    cursor.OnPointerMove(Vector2.zero);
                    backgroundCanvas.SetActive(true);
                    user.DisableGameInput();
                    user.EnableUIInput();
                    user.ResetMouseToCentre();
                    optionsPanel.Hide();
                    mainPanel.Show();
                    copyConfirmationText.color = new Color(1f, 1f, 1f, 0f);
                    break;
                case PauseMenuState.Unpaused:
                    Game.Instance.PauseGameToggle(false);
                    backgroundCanvas.SetActive(false);
                    user.EnableGameInput();
                    user.DisableUIInput();
                    user.ResetMouseToCentre();
                    break;
                case PauseMenuState.PausedOptionsMenu:
                    // TODO: use Progress instead (refactor this mad menu!!)
                    PlayOpenSound();
                    optionsPanel.Open(this);
                    mainPanel.Hide();
                    break;
            }
        }
    }
}