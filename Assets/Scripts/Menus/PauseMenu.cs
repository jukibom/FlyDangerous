using System.Collections;
using Audio;
using Menus.Options;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Menus {
    public enum PauseMenuState {
        Unpaused,
        PausedMainMenu,
        PausedOptionsMenu,
    }
    
    public class PauseMenu : MonoBehaviour {

        [Tooltip("Used to animate the main panel")] [SerializeField]
        private GameObject mainCanvas;
        
        [Tooltip("Used to show a background (if not VR)")] [SerializeField]
        private GameObject backgroundCanvas;

        [SerializeField] private PauseMainMenu mainPanel;

        [SerializeField] private OptionsMenu optionsPanel;

        [SerializeField] private Text copyConfirmationText;
        [SerializeField] private Text seedText;
        
        [SerializeField] private User user;

        private PauseMenuState _menuState = PauseMenuState.Unpaused;

        public bool IsPaused => _menuState != PauseMenuState.Unpaused;
        public PauseMenuState MenuState {
            get => _menuState;
            private set {
                _menuState = value;
                UpdatePauseGameState();
            }
        }
        private Canvas _menuCanvas;
        private Animator _panelAnimator;

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
        
        private void Start() {
            seedText.text = Game.Instance.IsTerrainMap ? "SEED: " + Game.Instance.Seed : "";
            _menuCanvas = backgroundCanvas.GetComponent<Canvas>();
            _panelAnimator = mainCanvas.GetComponent<Animator>();

            // TODO: Global VR Mode flag to turn the UI into a world space floating panel
            var VRMODE = false;
            if (VRMODE) {
                GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                GetComponent<Image>().enabled = false;
                // TODO: Detach from external camera? Maybe just keep the camera as-is but disable camera accel movements.
            }

            UpdatePauseGameState();
        }

        public void Pause() {
            AudioManager.Instance.Play("ui-dialog-open");
            MenuState = PauseMenuState.PausedMainMenu;
            mainPanel.HighlightResume();
            _panelAnimator.SetBool("Open", true);
        }

        public void Resume() {
            AudioManager.Instance.Play("ui-cancel");
            MenuState = PauseMenuState.Unpaused;
            _panelAnimator.SetBool("Open", false);
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
            GUIUtility.systemCopyBuffer = Game.Instance.LevelDataCurrent.ToJsonString();
            var copyConfirmTransform = copyConfirmationText.transform;
            copyConfirmTransform.localPosition = new Vector3(copyConfirmTransform.localPosition.x, 55, copyConfirmTransform.position.z);
            copyConfirmationText.color = new Color(1f, 1f, 1f, 1f);
            
            IEnumerator FadeText() {
                while (copyConfirmationText.color.a > 0.0f) {
                    copyConfirmationText.color = new Color(1f, 1f, 1f, copyConfirmationText.color.a - Time.unscaledDeltaTime);
                    
                    copyConfirmTransform.localPosition = new Vector3(
                        copyConfirmTransform.localPosition.x, 
                        copyConfirmationText.gameObject.transform.localPosition.y + (Time.unscaledDeltaTime * 20), 
                        copyConfirmTransform.position.z
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
                    Game.Instance.HideCursor();
                    backgroundCanvas.SetActive(false);
                    user.EnableGameInput();
                    user.DisableUIInput();
                    user.ResetMouseToCentre();
                    Time.timeScale = 1;
                    break;
                case PauseMenuState.PausedMainMenu: 
                    Game.Instance.ShowCursor();
                    backgroundCanvas.SetActive(true);
                    user.DisableGameInput();
                    user.EnableUIInput();
                    user.ResetMouseToCentre();
                    optionsPanel.Hide();
                    mainPanel.Show();
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