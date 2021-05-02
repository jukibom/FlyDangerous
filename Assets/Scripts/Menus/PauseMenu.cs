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
        
        [SerializeField] private User user;

        private PauseMenuState _menuState = PauseMenuState.Unpaused;
        
        public PauseMenuState MenuState {
            get => this._menuState;
            private set {
                _menuState = value;
                this.UpdatePauseGameState();
            }
        }
        private Canvas _menuCanvas;
        private Animator _panelAnimator;

        public void OnGameMenuToggle() {
            switch (this.MenuState) {
                case PauseMenuState.Unpaused:
                    Pause();
                    break;
                case PauseMenuState.PausedMainMenu:
                    Resume();
                    break;
                case PauseMenuState.PausedOptionsMenu:
                    this.optionsPanel.Cancel();
                    break;
            }
        }
        
        private void Start() {
            this._menuCanvas = this.backgroundCanvas.GetComponent<Canvas>();
            this._panelAnimator = this.mainCanvas.GetComponent<Animator>();

            // TODO: Global VR Mode flag to turn the UI into a world space floating panel
            var VRMODE = false;
            if (VRMODE) {
                GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                GetComponent<Image>().enabled = false;
                // TODO: Detach from external camera? Maybe just keep the camera as-is but disable camera accel movements.
            }

            this.UpdatePauseGameState();
        }

        public void Pause() {
            AudioManager.Instance.Play("ui-dialog-open");
            this.MenuState = PauseMenuState.PausedMainMenu;
            this.mainPanel.HighlightResume();
            this._panelAnimator.SetBool("Open", true);
        }

        public void Resume() {
            AudioManager.Instance.Play("ui-cancel");
            this.MenuState = PauseMenuState.Unpaused;
            this._panelAnimator.SetBool("Open", false);
        }

        public void Restart() {
            AudioManager.Instance.Play("ui-confirm");
            Resume();
            Game.Instance.RestartLevel();
        }

        public void OpenOptionsPanel() {
            AudioManager.Instance.Play("ui-dialog-open");
            this.MenuState = PauseMenuState.PausedOptionsMenu;
        }

        public void CloseOptionsPanel() {
            this.MenuState = PauseMenuState.PausedMainMenu;
            this.mainPanel.HighlightOptions();
        }

        public void Quit() {
            AudioManager.Instance.Play("ui-confirm");
            Resume();
            Game.Instance.QuitToMenu();
        }
        
        // toggle ship controller input and timescales
        private void UpdatePauseGameState() {
            switch (this.MenuState) {
                case PauseMenuState.Unpaused:
                    this.backgroundCanvas.SetActive(false);
                    this.user.EnableGameInput();
                    Time.timeScale = 1;
                    break;
                case PauseMenuState.PausedMainMenu: 
                    this.backgroundCanvas.SetActive(true);
                    this.user.DisableGameInput();
                    this.optionsPanel.Hide();
                    this.mainPanel.Show();
                    Time.timeScale = 0;
                    break;
                case PauseMenuState.PausedOptionsMenu:
                    this.optionsPanel.Show();
                    this.mainPanel.Hide();
                    break;
            }
        }
    }
}