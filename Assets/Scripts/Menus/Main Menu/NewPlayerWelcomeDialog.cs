using Core;
using UI;
using UnityEngine;

namespace Menus.Main_Menu {
    public class NewPlayerWelcomeDialog : MenuBase {
        [SerializeField] private GameObject autoRotateOption;
        [SerializeField] private GameObject flightAssistModeDropdown;

        [SerializeField] private GameObject autoRotateOptionDisabledFacade;
        [SerializeField] private GameObject flightAssistModeDropdownDisabledFacade;

        [SerializeField] private Checkbox autoShipRotation;
        [SerializeField] private DropdownOption defaultFlightAssist;

        [SerializeField] private TopMenu topMenu;

        private string _controlSchemeType = "arcade";

        private void Start() {
            SetControlSchemeOptions(false);
        }

        public void Accept() {
            Preferences.Instance.SetString("controlSchemeType", _controlSchemeType);
            if (_controlSchemeType == "advanced") {
                Preferences.Instance.SetBool("autoShipRotation", autoShipRotation.isChecked);
                if (autoShipRotation.isChecked) Preferences.Instance.SetString("flightAssistDefault", defaultFlightAssist.Value);
            }

            Preferences.Instance.Save();

            PlayApplySound();
            if (caller != null) caller.Open(topMenu);
            Hide();
        }

        public void OnControlSchemeToggle(string controlSchemeType) {
            _controlSchemeType = controlSchemeType;
            SetControlSchemeOptions(controlSchemeType == "advanced");
        }

        public void OnAutoRotationToggle(bool toggle) {
            SetFlightAssistVisibility(!toggle);
        }

        private void SetControlSchemeOptions(bool isAdvancedActive) {
            SetAdvancedOptionsVisibility(isAdvancedActive);
            SetArcadeOptionsVisibility(!isAdvancedActive);
            SetFlightAssistVisibility(isAdvancedActive && !autoShipRotation.isChecked);
        }

        private void SetAdvancedOptionsVisibility(bool show) {
            autoRotateOption.SetActive(show);
        }

        private void SetArcadeOptionsVisibility(bool show) {
            autoRotateOptionDisabledFacade.SetActive(show);
        }

        private void SetFlightAssistVisibility(bool shouldShow) {
            flightAssistModeDropdown.SetActive(shouldShow);
            flightAssistModeDropdownDisabledFacade.SetActive(!shouldShow);
        }
    }
}