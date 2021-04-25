using System;
using Audio;
using Engine;
using UI;
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
        private SaveData _previousPrefs;
        
        private void Awake() {
            this._animator = this.GetComponent<Animator>();
        }

        private void Start() {
            LoadPreferences();
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
            SavePreferences();
            this.pauseMenu.CloseOptionsPanel();
            AudioManager.Instance.Play("ui-confirm");
        }

        public void Cancel() {
            
            // not sure about this - the hack here is to save the preferences, compare with previous and revert is the user chooses to discard.
            SavePreferences();
            if (_previousPrefs.ToJsonString() != Preferences.Instance.GetCurrent().ToJsonString()) {
                Debug.Log("Discarded changed preferences! (TODO: confirmation dialog)");
                // TODO: Confirmation dialog (if there is state to commit)
            }
            
            RevertPreferences();
            AudioManager.Instance.Play("ui-cancel");
            this.pauseMenu.CloseOptionsPanel();
        }

        private void LoadPreferences() {
            LoadBindings();
            
            var toggleOptions = GetComponentsInChildren<ToggleOption>(true);
            foreach (var toggleOption in toggleOptions) {
                toggleOption.IsEnabled = Preferences.Instance.GetBool(toggleOption.Preference);
            }

            _previousPrefs = Preferences.Instance.GetCurrent().Clone();
        }

        private void RevertPreferences() {
            Preferences.Instance.SetPreferences(_previousPrefs);
            LoadBindings();
        }

        private void SavePreferences() {

            Preferences.Instance.SetString("inputBindings", actions.SaveBindingOverridesAsJson());

            var toggleOptions = GetComponentsInChildren<ToggleOption>(true);
            foreach (var toggleOption in toggleOptions) {
                Preferences.Instance.SetBool(toggleOption.Preference, toggleOption.IsEnabled);
            }
            
            Preferences.Instance.Save();
        }
        private void LoadBindings() {
            var bindings = Preferences.Instance.GetString("inputBindings");
            if (!string.IsNullOrEmpty(bindings)) {
                actions.LoadBindingOverridesFromJson(bindings);
            }
        }
    }
}