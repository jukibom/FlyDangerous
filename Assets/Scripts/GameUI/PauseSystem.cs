using Core;
using Core.Player;
using JetBrains.Annotations;
using Menus.Pause_Menu;
using UnityEngine;

namespace GameUI {
    public class PauseSystem : MonoBehaviour {
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private User user;
        [SerializeField] private GameObject menuContainer;

        public bool IsPaused { get; private set; }

        private void Awake() {
            menuContainer.SetActive(false);
        }

        [UsedImplicitly]
        public void OnGameMenuToggle() {
            IsPaused = !IsPaused;
            if (IsPaused) {
                Game.Instance.PauseGameToggle(true);
                menuContainer.SetActive(true);
                user.DisableGameInput();
                user.EnableUIInput();
                user.ResetMouseToCentre();
                pauseMenu.Open(null);
                pauseMenu.PlayOpenSound();
            }
            else {
                Game.Instance.PauseGameToggle(false);
                menuContainer.SetActive(false);
                user.EnableGameInput();
                user.DisableUIInput();
                user.ResetMouseToCentre();
            }
        }

        public void Resume() {
            OnGameMenuToggle();
        }

        public void Restart() {
            Resume();
            Game.Instance.RestartSession();
        }

        public void Quit() {
            Resume();
            Game.Instance.LeaveSession();
        }

        // toggle ship controller input and timescales
        // private void UpdatePauseGameState() {
        //     Debug.Log(MenuState);
        //     switch (MenuState) {
        //         case PauseMenuState.PausedMainMenu:
        //             Game.Instance.PauseGameToggle(true);
        //             cursor.OnPointerMove(Vector2.zero);
        //             _mainCanvas.enabled = true;
        //             user.DisableGameInput();
        //             user.EnableUIInput();
        //             user.ResetMouseToCentre();
        //             optionsPanel.Hide();
        //             mainPanel.Show();
        //             copyConfirmationText.color = new Color(1f, 1f, 1f, 0f);
        //             break;
        //         case PauseMenuState.Unpaused:
        //             Game.Instance.PauseGameToggle(false);
        //             _mainCanvas.enabled = false;
        //             user.EnableGameInput();
        //             user.DisableUIInput();
        //             user.ResetMouseToCentre();
        //             break;
        //         case PauseMenuState.PausedOptionsMenu:
        //             // TODO: use Progress instead (refactor this mad menu!!)
        //             PlayOpenSound();
        //             optionsPanel.Open(this);
        //             mainPanel.Hide();
        //             break;
        //     }
        // }
    }
}