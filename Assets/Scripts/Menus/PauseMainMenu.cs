using UnityEngine;
using UnityEngine.UI;

namespace Menus {
    public class PauseMainMenu : MonoBehaviour {
        
        [SerializeField]
        private PauseMenu pauseMenu;

        [SerializeField]
        private Button resumeButton;
        
        [SerializeField]
        private Button optionsButton;

        private Animator _animator;
        
        private void Awake() {
            this._animator = this.GetComponent<Animator>();
        }
        public void Show() {
            this.gameObject.SetActive(true);
            this._animator.SetBool("Open", true);
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