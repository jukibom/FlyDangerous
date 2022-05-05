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
            Game.Instance.PauseGameToggle(IsPaused);
            AudioMixer.Instance.SetMusicLowPassEnabled(IsPaused);
            menuContainer.SetActive(IsPaused);

            if (IsPaused) {
                user.DisableGameInput();
                user.EnableUIInput();
                user.ResetMouseToCentre();
                pauseMenu.Open(null);
                pauseMenu.PlayOpenSound();
            }
            else {
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
    }
}