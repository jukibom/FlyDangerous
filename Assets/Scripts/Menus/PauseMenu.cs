using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Menus {
    public enum PauseMenuState {
        Unpaused,
        PausedMainMenu,
        PausedOptionsMenu,
    }
    
    [RequireComponent(typeof(Canvas))]
    public class PauseMenu : MonoBehaviour {

        [Tooltip("Used to animate the main panel")] [SerializeField]
        private GameObject pauseMenuCanvas;

        [SerializeField]
        private MainMenu mainPanel;

        [SerializeField]
        private OptionsMenu optionsPanel;

        [SerializeField] 
        private EventSystem eventSystem;

        private PauseMenuState _menuState = PauseMenuState.Unpaused;

        private FlyDangerousActions.ShipActions _shipInput;
        private InputActionAsset _uiInput;
        
        public PauseMenuState MenuState {
            get => this._menuState;
            private set {
                _menuState = value;
                this.UpdatePauseGameState();
            }
        }
        private FlyDangerousActions _gameActions;
        private Canvas _menuCanvas;
        private Animator _panelAnimator;

        private void Awake() {
            this._menuCanvas = GetComponent<Canvas>();
            this._panelAnimator = this.pauseMenuCanvas.GetComponent<Animator>();
            
            this._gameActions = GlobalGameState.Actions;
            this._gameActions.Global.Enable();
            this._shipInput = this._gameActions.Ship;
            this._uiInput = this.eventSystem.GetComponent<InputSystemUIInputModule>().actionsAsset;

            this._gameActions.Global.GameMenuToggle.performed += ToggleMenuAction;
            this._gameActions.Global.GameMenuToggle.canceled += ToggleMenuAction;

            this.UpdatePauseGameState();
        }

        public void Pause() {
            AudioManager.Instance.Play("ui-select");
            this.MenuState = PauseMenuState.PausedMainMenu;
            this.mainPanel.HighlightResume();
            this._panelAnimator.SetBool("Open", true);
        }

        public void Resume() {
            AudioManager.Instance.Play("ui-nav-secondary");
            this.MenuState = PauseMenuState.Unpaused;
            this._panelAnimator.SetBool("Open", false);
            this._panelAnimator.Play("Standby");
        }

        public void Restart() {
            AudioManager.Instance.Play("ui-select");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OpenOptionsPanel() {
            AudioManager.Instance.Play("ui-select");
            this.MenuState = PauseMenuState.PausedOptionsMenu;
        }

        public void CloseOptionsPanel() {
            AudioManager.Instance.Play("ui-nav-secondary");
            this.MenuState = PauseMenuState.PausedMainMenu;
            this.mainPanel.HighlightOptions();
        }

        public void Quit() {
            AudioManager.Instance.Play("ui-select");
            // TODO: Confirmation dialog
            Application.Quit();
        }

        private void ToggleMenuAction(InputAction.CallbackContext context) {
            if (context.ReadValueAsButton()) {
                switch (this.MenuState) {
                    case PauseMenuState.Unpaused:
                        Pause();
                        break;
                    case PauseMenuState.PausedMainMenu: 
                        Resume();
                        break;
                    case PauseMenuState.PausedOptionsMenu:
                        CloseOptionsPanel();
                        break;
                }
            }
        }

        // toggle ship controller input and timescales
        private void UpdatePauseGameState() {
            switch (this.MenuState) {
                case PauseMenuState.Unpaused:
                    this._menuCanvas.enabled = false;
                    this._shipInput.Enable();
                    this._uiInput.Disable();
                    Time.timeScale = 1;
                    break;
                case PauseMenuState.PausedMainMenu: 
                    this._menuCanvas.enabled = true;
                    this._shipInput.Disable();
                    this._uiInput.Enable();
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