using System.Collections;
using Core;
using UI;
using UnityEngine;

namespace Menus.Options {
    /**
     * This whole class is a big gross ball of mud. It's used in one place and literally nobody will read this anyway.
     * This basically just controls the tab and option visibility in the main input panel. It uses some super gross fake hidden versions of two
     * options instead of changing the look of the existing ones because I'm extremely lazy and so done with this feature now.
     */
    public class Controls : MonoBehaviour {
        [SerializeField] private FdToggleGroup controlSchemeToggleGroup;
        [SerializeField] private GameObject bindingsTab;
        [SerializeField] private GameObject mouseSettingsTab;
        [SerializeField] private GameObject controlsLayoutTab;

        [SerializeField] private GameObject autoRotateOption;
        [SerializeField] private GameObject flightAssistModeDropdown;

        [SerializeField] private GameObject autoRotateOptionDisabledFacade;
        [SerializeField] private GameObject flightAssistModeDropdownDisabledFacade;

        [SerializeField] private Checkbox autoShipRoll;
        [SerializeField] private Checkbox autoShipRotation;

        private bool _isAdvancedEnabled;

        public void OnEnable() {
            // wait a frame to allow everything to load 
            IEnumerator InitControlSchemeOptions() {
                yield return new WaitForEndOfFrame();
                var controlSchemeType = Preferences.Instance.GetString("controlSchemeType");
                controlSchemeToggleGroup.Value = controlSchemeType;
                SetControlSchemeOptions(controlSchemeType == "advanced");
            }

            StartCoroutine(InitControlSchemeOptions());
        }

        public void OnControlSchemeToggle(string controlSchemeType) {
            SetControlSchemeOptions(controlSchemeType == "advanced");
        }

        public void OnAutoRollToggle(bool toggle) {
            SetAutoRotateVisibility(_isAdvancedEnabled && toggle == false);
            SetFlightAssistVisibility(_isAdvancedEnabled && !autoShipRotation.isChecked && toggle == false);
        }

        public void OnAutoRotationToggle(bool toggle) {
            SetFlightAssistVisibility(!toggle);
        }

        private void SetControlSchemeOptions(bool isAdvancedActive) {
            _isAdvancedEnabled = isAdvancedActive;
            SetAdvancedOptionsVisibility(isAdvancedActive);
            SetArcadeOptionsVisibility(!isAdvancedActive);
            SetAutoRotateVisibility(isAdvancedActive && !autoShipRoll.isChecked);
            SetFlightAssistVisibility(isAdvancedActive && !autoShipRotation.isChecked && !autoShipRoll.isChecked);
        }

        private void SetAdvancedOptionsVisibility(bool show) {
            mouseSettingsTab.SetActive(show);
            bindingsTab.SetActive(show);
            autoRotateOption.SetActive(show);
        }

        private void SetArcadeOptionsVisibility(bool show) {
            controlsLayoutTab.SetActive(show);
            autoRotateOptionDisabledFacade.SetActive(show);
        }

        private void SetAutoRotateVisibility(bool shouldShow) {
            autoRotateOption.SetActive(shouldShow);
            autoRotateOptionDisabledFacade.SetActive(!shouldShow);
        }

        private void SetFlightAssistVisibility(bool shouldShow) {
            flightAssistModeDropdown.SetActive(shouldShow);
            flightAssistModeDropdownDisabledFacade.SetActive(!shouldShow);
        }
    }
}