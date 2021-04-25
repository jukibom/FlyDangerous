using System;
using Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menus.Options {

    public class OptionsMenu : MonoBehaviour {
        [SerializeField] 
        private PauseMenu pauseMenu;
        public InputActionAsset actions;

        [SerializeField] private Button defaultSelectedButton;
        
        private Animator _animator;
        private string _prefs;
        
        private void Awake() {
            this._animator = this.GetComponent<Animator>();
        }
        private void OnEnable() {
            defaultSelectedButton.Select();
            LoadPreferences();
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
            // TODO: Store preference state here
            SaveBindings();
            this.pauseMenu.CloseOptionsPanel();
            AudioManager.Instance.Play("ui-confirm");
        }

        public void Cancel() {
            // TODO: Confirmation dialog (if there is state to commit)
            LoadBindings();
            AudioManager.Instance.Play("ui-cancel");
            this.pauseMenu.CloseOptionsPanel();
        }

        private void LoadPreferences() {
            // TODO: load preferences here (ideally from json ¬_¬)
            LoadBindings();
        }

        private void RevertPreferences() {
            LoadBindings();
        }

        private void SaveBindings() {
            var rebinds = actions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString("rebinds", rebinds);
        }
        private void LoadBindings() {
            var bindings = PlayerPrefs.GetString("rebinds");
            if (!string.IsNullOrEmpty(bindings)) {
                actions.LoadBindingOverridesFromJson(bindings);
            }
        }
    }
}