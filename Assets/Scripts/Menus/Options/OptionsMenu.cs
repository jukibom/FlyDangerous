using System;
using Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Menus {
    public class OptionsMenu : MonoBehaviour {
        [SerializeField] 
        private PauseMenu pauseMenu;

        [SerializeField] private Button defaultSelectedButton;
        
        private Animator _animator;
        
        private void Awake() {
            this._animator = this.GetComponent<Animator>();
        }
        private void OnEnable() {
            defaultSelectedButton.Select();
        }

        public void Show() {
            gameObject.SetActive(true);
            this._animator.SetBool("Open", true);
        }

        public void Hide() {
            this.gameObject.SetActive(false);
            // TODO: Animate out and set active false on complete (how?!)
            // this._animator.SetBool("Open", false);
        }

        public void Apply() {
            // TODO: Store state here
            this.pauseMenu.CloseOptionsPanel();
            AudioManager.Instance.Play("ui-confirm");
        }

        public void Cancel() {
            // TODO: Confirmation dialog (if there is state to commit)
            AudioManager.Instance.Play("ui-cancel");
            this.pauseMenu.CloseOptionsPanel();
        }
    }
}