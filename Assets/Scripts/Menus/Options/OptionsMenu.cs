using Core;
using Core.Player;
using CustomWebSocketSharp;
using FdUI;
using Menus.Main_Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menus.Options {
    public class OptionsMenu : MenuBase {
        [SerializeField] private OptionsConfirmationDialog optionsConfirmationDialog;
        [SerializeField] private Button cancelButton;

        public InputActionAsset actions;

        private bool _flightAssistDefaultsChanged;

        private SaveData _previousPrefs;
        private SaveData _pendingPrefs;
        private string _startingBindings;

        protected override void OnOpen() {
            _startingBindings = Preferences.Instance.GetString("inputBindings");

            if (_pendingPrefs != null) {
                Preferences.Instance.SetPreferences(_pendingPrefs);
                _pendingPrefs = null;
            }

            LoadPreferences();
            var newPrefs = GetPreferencesFromOptionsPanel();
            Preferences.Instance.SetPreferences(newPrefs);

            if (_previousPrefs != null) Preferences.Instance.SetPreferences(_previousPrefs);

            _previousPrefs = Preferences.Instance.GetCurrent().Clone();
            optionsConfirmationDialog.Hide();
        }

        public void ToggleVR() {
            if (Game.IsVREnabled)
                Game.Instance.DisableVRIfNeeded();
            else
                Game.Instance.EnableVR();
        }

        public override void Cancel() {
            Cancel(false);
        }

        public void Cancel(bool force) {
            if (force) {
                _previousPrefs = null;
                _pendingPrefs = null;
                if (!_startingBindings.IsNullOrEmpty()) {
                    Preferences.Instance.SetString("inputBindings", _startingBindings);
                    _startingBindings = null;
                }

                base.Cancel();
                return;
            }

            if (EventSystem.current.currentSelectedGameObject.GetInstanceID() == cancelButton.gameObject.GetInstanceID()) {
                var newPrefs = GetPreferencesFromOptionsPanel();
                var hasChanges = _previousPrefs.ToJsonString() != newPrefs.ToJsonString();

                if (hasChanges) {
                    _pendingPrefs = newPrefs;
                    Progress(optionsConfirmationDialog);
                }
                else {
                    _previousPrefs = null;
                    _pendingPrefs = null;
                    base.Cancel();
                }
            }
            else {
                cancelButton.Select();
            }
        }

        public void Apply() {
            _previousPrefs = null;
            var newPrefs = GetPreferencesFromOptionsPanel();
            Preferences.Instance.SetPreferences(newPrefs);
            Preferences.Instance.Save();

            SetDebugFlightParameters();
            Game.Instance.ApplyGameOptions();
            if (_flightAssistDefaultsChanged) {
                var player = FdPlayer.FindLocalShipPlayer;
                if (player) player.SetFlightAssistFromDefaults();
            }

            Progress(caller, false, false);
        }

        public void OnFlightAssistDefaultsChange(Dropdown dropdown) {
            _flightAssistDefaultsChanged = Preferences.Instance.GetString("flightAssistDefault") != GetFlightAssistDefaultPreference(dropdown.value);
        }

        private void LoadPreferences() {
            Debug.Log("Load preferences");

            Game.Instance.LoadBindings();

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
        }

        // Save all the settings but don't maintain that state in prefs and don't serialize to disk
        private SaveData GetPreferencesFromOptionsPanel() {
            var existing = Preferences.Instance.GetCurrent().Clone();

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

            var newPrefs = Preferences.Instance.GetCurrent();
            Preferences.Instance.SetPreferences(existing);

            return newPrefs;
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