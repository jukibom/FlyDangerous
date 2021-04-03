using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private GameObject mainPanel;

        [SerializeField]
        private GameObject optionsPanel;

        private PauseMenuState _menuState = PauseMenuState.Unpaused;
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

            this._gameActions.Global.GameMenuToggle.performed += ToggleMenuAction;
            this._gameActions.Global.GameMenuToggle.canceled += ToggleMenuAction;

            this.UpdatePauseGameState();
        }

        public void Pause() {
            this.MenuState = PauseMenuState.PausedMainMenu;
            this._panelAnimator.SetBool("Open", true);
        }

        public void Resume() {
            this.MenuState = PauseMenuState.Unpaused;
            this._panelAnimator.SetBool("Open", false);
            this._panelAnimator.Play("Standby");
        }

        public void Restart() {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OpenOptionsPanel() {
            this.MenuState = PauseMenuState.PausedOptionsMenu;
        }

        public void CloseOptionsPanel() {
            this.MenuState = PauseMenuState.PausedMainMenu;
        }

        public void Quit() {
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
                    this._gameActions.Ship.Enable();
                    Time.timeScale = 1;
                    break;
                case PauseMenuState.PausedMainMenu: 
                    this._menuCanvas.enabled = true;
                    this._gameActions.Ship.Disable();
                    this.optionsPanel.SetActive(false);
                    this.mainPanel.SetActive(true);
                    Time.timeScale = 0;
                    break;
                case PauseMenuState.PausedOptionsMenu:
                    this.optionsPanel.SetActive(true);
                    this.mainPanel.SetActive(false);
                    break;
            }
        }
    }
}