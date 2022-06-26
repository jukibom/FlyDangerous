using Core;
using Core.Player;
using Menus.Main_Menu;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menus.Options {
    public class OptionsMenu : MenuBase {
        public InputActionAsset actions;
        private bool _flightAssistDefaultsChanged;
        private SaveData _previousPrefs;

        protected override void OnOpen() {
            LoadPreferences();
        }

        public void ToggleVR() {
            if (Game.Instance.IsVREnabled)
                Game.Instance.DisableVRIfNeeded();
            else
                Game.Instance.EnableVR();
        }

        public void Apply() {
            SavePreferences();
            SetDebugFlightParameters();
            Game.Instance.ApplyGameOptions();
            if (_flightAssistDefaultsChanged) {
                var player = FdPlayer.FindLocalShipPlayer;
                if (player) player.SetFlightAssistFromDefaults();
            }

            Progress(caller, false, false);
        }

        protected override void OnClose() {
            // not sure about this - the hack here is to save the preferences, compare with previous and revert if the user chooses to discard.
            SavePreferences();
            if (_previousPrefs.ToJsonString() != Preferences.Instance.GetCurrent().ToJsonString())
                Debug.Log("Discarded changed preferences! (TODO: confirmation dialog)");
            // TODO: Confirmation dialog (if there is state to commit)

            RevertPreferences();
            Preferences.Instance.Save();
        }

        public void OnFlightAssistDefaultsChange(Dropdown dropdown) {
            _flightAssistDefaultsChanged = Preferences.Instance.GetString("flightAssistDefault") != GetFlightAssistDefaultPreference(dropdown.value);
        }

        private void LoadPreferences() {
            Debug.Log("Load preferences");
            var toggleOptions = GetComponentsInChildren<ToggleOption>(true);
            foreach (var toggleOption in toggleOptions) toggleOption.IsEnabled = Preferences.Instance.GetBool(toggleOption.Preference);

            var dropdownOptions = GetComponentsInChildren<DropdownOption>(true);
            foreach (var dropdownOption in dropdownOptions)
                if (dropdownOption.savePreference)
                    dropdownOption.Value = Preferences.Instance.GetString(dropdownOption.Preference);

            var sliderOptions = GetComponentsInChildren<SliderOption>(true);
            foreach (var sliderOption in sliderOptions) sliderOption.Value = Preferences.Instance.GetFloat(sliderOption.preference);

            var toggleRadialOptions = GetComponentsInChildren<FdToggleGroup>(true);
            foreach (var toggleRadialOption in toggleRadialOptions)
                toggleRadialOption.Value = Preferences.Instance.GetString(toggleRadialOption.Preference);
            _previousPrefs = Preferences.Instance.GetCurrent().Clone();
        }

        private void RevertPreferences() {
            Preferences.Instance.SetPreferences(_previousPrefs);
            Game.Instance.LoadBindings();
        }

        private void SavePreferences() {
            Preferences.Instance.SetString("inputBindings", actions.SaveBindingOverridesAsJson());

            var toggleOptions = GetComponentsInChildren<ToggleOption>(true);
            foreach (var toggleOption in toggleOptions) Preferences.Instance.SetBool(toggleOption.Preference, toggleOption.IsEnabled);

            var dropdownOptions = GetComponentsInChildren<DropdownOption>(true);
            foreach (var dropdownOption in dropdownOptions)
                if (dropdownOption.savePreference)
                    Preferences.Instance.SetString(dropdownOption.Preference, dropdownOption.Value);

            var sliderOptions = GetComponentsInChildren<SliderOption>(true);
            foreach (var sliderOption in sliderOptions) Preferences.Instance.SetFloat(sliderOption.preference, sliderOption.Value);

            var toggleRadialOptions = GetComponentsInChildren<FdToggleGroup>(true);
            foreach (var toggleRadialOption in toggleRadialOptions) Preferences.Instance.SetString(toggleRadialOption.Preference, toggleRadialOption.Value);


            Preferences.Instance.Save();
        }

        private void SetDebugFlightParameters() {
            var debugFlightOptions = GetComponentInChildren<DevPanel>(true);
            if (debugFlightOptions) Game.Instance.ShipParameters = debugFlightOptions.GetFlightParams();
        }

        private string GetFlightAssistDefaultPreference(int dropdownValue) {
            var preference = "";
            switch (dropdownValue) {
                case 0:
                    preference = "all on";
                    break;
                case 1:
                    preference = "rotational assist only";
                    break;
                case 2:
                    preference = "vector assist only";
                    break;
                case 3:
                    preference = "all off";
                    break;
            }

            return preference;
        }
    }
}