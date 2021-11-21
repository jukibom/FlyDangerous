using System;
using Core;
using Core.MapData;
using Core.Player;
using Menus.Pause_Menu;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Pause_Menu {
    public class PauseMainMenu : MonoBehaviour {
        
        [SerializeField]
        private PauseMenu pauseMenu;

        [SerializeField]
        private Button resumeButton;
        
        [SerializeField]
        private Button restartButton;
        
        [SerializeField]
        private Button optionsButton;
        
        [SerializeField]
        private Button quitButton;

        private static readonly int open = Animator.StringToHash("Open");

        public void OnEnable() {
            // multiplayer specific UI changes
            var player = ShipPlayer.FindLocal;
            if (player && Game.Instance.SessionType == SessionType.Multiplayer) {
                // in free roam, restart for clients is changed to warping to the leader (on non-host client)
                if (!player.isHost && Game.Instance.LoadedLevelData.gameType.CanWarpToHost) {
                    restartButton.GetComponent<UIButton>().label.text = "WARP TO HOST";
                }
                quitButton.GetComponent<UIButton>().label.text = "LEAVE GAME";
                if (player.isHost) {
                    quitButton.GetComponent<UIButton>().label.text = "RETURN TO LOBBY";
                }
            }
        }

        public void Show() {
            gameObject.SetActive(true);
            GetComponent<Animator>().SetBool(open, true);
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        public void Resume() {
            pauseMenu.Resume();
        }

        public void Restart() {
            pauseMenu.Restart();
        }

        public void Options() {
            pauseMenu.OpenOptionsPanel();
        }

        public void Quit() {
            pauseMenu.Quit();
        }
        
        // This is gross but I'm not spending more time on this nonsense than I have to
        public void HighlightResume() {
            resumeButton.Select();
        }

        public void HighlightOptions() {
            optionsButton.Select();
        }
    }
}