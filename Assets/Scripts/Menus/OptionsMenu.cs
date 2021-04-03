using System;
using UnityEngine;
using UnityEngine.UI;

namespace Menus {
    public class OptionsMenu : MonoBehaviour {
        [SerializeField] 
        private PauseMenu pauseMenu;

        [SerializeField] private Button defaultSelectedButton;

        private void OnEnable() {
            defaultSelectedButton.Select();
        }

        public void Show() {
            // TODO: Animation!
            gameObject.SetActive(true);
        }

        public void Hide() {
            // TODO: Animation!
            gameObject.SetActive(false);
        }

        public void Apply() {
            // TODO: Store state here
            this.pauseMenu.CloseOptionsPanel();
        }

        public void Cancel() {
            // TODO: Confirmation dialog (if there is state to commit)
            this.pauseMenu.CloseOptionsPanel();
        }
    }
}