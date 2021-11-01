using System;
using Audio;
using Core;
using Core.Player;
using Menus.Main_Menu;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menus.Options {
    
    public class OptionsMenu : MenuBase {
        public InputActionAsset actions;
        private SaveData _previousPrefs;

        protected override void OnOpen() {
            LoadPreferences();
        }

        public void ToggleVR() {
            if (Game.Instance.IsVREnabled) {
                Game.Instance.DisableVRIfNeeded();
            }
            else {
                Game.Instance.EnableVR();
            }
        }

        public void Apply() {
            SavePreferences();
            SetDebugFlightParameters();
            Game.Instance.ApplyGameOptions();
            Progress(caller, false, false);
        }

        protected override void OnCancel() {
            // not sure about this - the hack here is to save the preferences, compare with previous and revert is the user chooses to discard.
            SavePreferences();
            if (_previousPrefs.ToJsonString() != Preferences.Instance.GetCurrent().ToJsonString()) {
                Debug.Log("Discarded changed preferences! (TODO: confirmation dialog)");
                // TODO: Confirmation dialog (if there is state to commit)
            }
            
            RevertPreferences();
            SavePreferences();
        }

        public void OnFlightAssistDefaultsChange(Dropdown dropdown) {
            
            // if the game is running, apply the chosen defaults to the local player ship
            var player = ShipPlayer.FindLocal;
            if (player) {
                var preference = "";
                switch (dropdown.value) {
                    case 0: preference = "all on"; break;
                    case 1: preference = "rotational assist only"; break;
                    case 2: preference = "vector assist only"; break;
                    case 3: preference = "all off"; break;
                }
                player.SetFlightAssistDefaults(preference);
            }
        }

        private void LoadPreferences() {
            var toggleOptions = GetComponentsInChildren<ToggleOption>(true);
            foreach (var toggleOption in toggleOptions) {
                toggleOption.IsEnabled = Preferences.Instance.GetBool(toggleOption.Preference);
            }
            
            var dropdownOptions = GetComponentsInChildren<DropdownOption>(true);
            foreach (var dropdownOption in dropdownOptions) {
                if (dropdownOption.savePreference) {
                    dropdownOption.Value = Preferences.Instance.GetString(dropdownOption.Preference);
                }
            }
            
            var sliderOptions = GetComponentsInChildren<SliderOption>(true);
            foreach (var sliderOption in sliderOptions) {
                sliderOption.Value = Preferences.Instance.GetFloat(sliderOption.preference);
            }

            _previousPrefs = Preferences.Instance.GetCurrent().Clone();
        }

        private void RevertPreferences() {
            Preferences.Instance.SetPreferences(_previousPrefs);
            Game.Instance.LoadBindings();
        }

        private void SavePreferences() {

            Preferences.Instance.SetString("inputBindings", actions.SaveBindingOverridesAsJson());

            var toggleOptions = GetComponentsInChildren<ToggleOption>(true);
            foreach (var toggleOption in toggleOptions) {
                Preferences.Instance.SetBool(toggleOption.Preference, toggleOption.IsEnabled);
            }

            var dropdownOptions = GetComponentsInChildren<DropdownOption>(true);
            foreach (var dropdownOption in dropdownOptions) {
                if (dropdownOption.savePreference) {
                    Preferences.Instance.SetString(dropdownOption.Preference, dropdownOption.Value);
                }
            }
            
            var sliderOptions = GetComponentsInChildren<SliderOption>(true);
            foreach (var sliderOption in sliderOptions) {
                Preferences.Instance.SetFloat(sliderOption.preference, sliderOption.Value);
            }

            Preferences.Instance.Save();
        }

        private void SetDebugFlightParameters() {
            var debugFlightOptions = GetComponentInChildren<DevPanel>(true);
            if (debugFlightOptions) {
                Game.Instance.ShipParameters = debugFlightOptions.GetFlightParams();
            }
        }
    }
}