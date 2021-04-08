using Audio;
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
    
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(PlayerInput))]
    public class PauseMenu : MonoBehaviour {

        [Tooltip("Used to animate the main panel")] [SerializeField]
        private GameObject pauseMenuCanvas;

        [SerializeField] private MainMenu mainPanel;

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
            ToggleMenuAction();
        }
        
        private void Start() {
            this._menuCanvas = GetComponent<Canvas>();
            this._panelAnimator = this.pauseMenuCanvas.GetComponent<Animator>();

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
            this._panelAnimator.Play("Standby");
        }

        public void Restart() {
            AudioManager.Instance.Play("ui-confirm");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
            // TODO: Confirmation dialog
            Application.Quit();
        }

        private void ToggleMenuAction() {
            // if (context.ReadValueAsButton()) {
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
            // }
        }

        // toggle ship controller input and timescales
        private void UpdatePauseGameState() {
            switch (this.MenuState) {
                case PauseMenuState.Unpaused:
                    this._menuCanvas.enabled = false;
                    this.user.EnableGameInput();
                    Time.timeScale = 1;
                    break;
                case PauseMenuState.PausedMainMenu: 
                    this._menuCanvas.enabled = true;
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