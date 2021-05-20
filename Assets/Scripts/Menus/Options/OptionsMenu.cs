using System;
using Audio;
using Engine;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menus.Options {
    
    public class OptionsMenu : MonoBehaviour {
        [SerializeField] 
        private UnityEvent returnToParentMenu;
        
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
            SetDebugFlightParameters();
            returnToParentMenu.Invoke();
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
            SavePreferences();
            AudioManager.Instance.Play("ui-cancel");
            returnToParentMenu.Invoke();
        }

        private void LoadPreferences() {
            LoadBindings();
            
            var toggleOptions = GetComponentsInChildren<ToggleOption>(true);
            foreach (var toggleOption in toggleOptions) {
                toggleOption.IsEnabled = Preferences.Instance.GetBool(toggleOption.Preference);
            }
            
            var dropdownOptions = GetComponentsInChildren<DropdownOption>(true);
            foreach (var dropdownOption in dropdownOptions) {
                dropdownOption.Value = Preferences.Instance.GetString(dropdownOption.Preference);
            }
            
            var sliderOptions = GetComponentsInChildren<SliderOption>(true);
            foreach (var sliderOption in sliderOptions) {
                sliderOption.Value = Preferences.Instance.GetFloat(sliderOption.preference);
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

            var dropdownOptions = GetComponentsInChildren<DropdownOption>(true);
            foreach (var dropdownOption in dropdownOptions) {
                Preferences.Instance.SetString(dropdownOption.Preference, dropdownOption.Value);
            }
            
            var sliderOptions = GetComponentsInChildren<SliderOption>(true);
            foreach (var sliderOption in sliderOptions) {
                Preferences.Instance.SetFloat(sliderOption.preference, sliderOption.Value);
            }

            // TODO: mouse sensitivity (save defaults here so it writes to config for now)
            Preferences.Instance.SetFloat("mouseXSensitivity", Preferences.Instance.GetFloat("mouseXSensitivity"));
            Preferences.Instance.SetFloat("mouseYSensitivity", Preferences.Instance.GetFloat("mouseYSensitivity"));
            
            Preferences.Instance.Save();
        }

        private void SetDebugFlightParameters() {
            var debugFlightOptions = GetComponent<DevPanelFlightParams>();
            if (debugFlightOptions) {
                Game.Instance.ShipParameters = debugFlightOptions.GetFlightParams();
            }
        }
        private void LoadBindings() {
            var bindings = Preferences.Instance.GetString("inputBindings");
            if (!string.IsNullOrEmpty(bindings)) {
                actions.LoadBindingOverridesFromJson(bindings);
            }
        }
    }
}