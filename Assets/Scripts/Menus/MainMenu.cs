using UnityEngine;
using UnityEngine.UI;

namespace Menus {
    public class MainMenu : MonoBehaviour {
        
        [SerializeField]
        private PauseMenu pauseMenu;

        [SerializeField]
        private Button resumeButton;
        
        [SerializeField]
        private Button optionsButton;
        
        public void Show() {
            // TODO: Animation?
            this.gameObject.SetActive(true);
        }

        public void Hide() {
            this.gameObject.SetActive(false);
        }

        public void Resume() {
            this.pauseMenu.Resume();
        }

        public void Restart() {
            this.pauseMenu.Restart();
        }

        public void Options() {
            this.pauseMenu.OpenOptionsPanel();
        }

        public void Quit() {
            this.pauseMenu.Quit();
        }
        
        // This is gross but I'm not spending more time on this nonsense than I have to
        public void HighlightResume() {
            this.resumeButton.Select();
        }

        public void HighlightOptions() {
            this.optionsButton.Select();
        }
    }
}