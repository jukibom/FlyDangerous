using System.Collections;
using Core;
using UI;
using UnityEngine;

namespace Menus.Options {
    public class Controls : MonoBehaviour {
        [SerializeField] private FdToggleGroup controlSchemeToggleGroup;
        [SerializeField] private GameObject bindingsTab;
        [SerializeField] private GameObject mouseSettingsTab;
        [SerializeField] private GameObject controlsLayoutTab;
        [SerializeField] private GameObject autoRotateOption;
        [SerializeField] private GameObject flightAssistModeDropdown;

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

        private void SetControlSchemeOptions(bool isAdvancedActive) {
            Debug.Log("Is advanced enabled? " + isAdvancedActive);
            SetAdvancedOptionsVisibility(isAdvancedActive);
            SetArcadeOptionsVisibility(!isAdvancedActive);
        }

        private void SetAdvancedOptionsVisibility(bool show) {
            mouseSettingsTab.SetActive(show);
            bindingsTab.SetActive(show);
            autoRotateOption.SetActive(show);
            flightAssistModeDropdown.SetActive(show);
        }

        private void SetArcadeOptionsVisibility(bool show) {
            controlsLayoutTab.SetActive(show);
        }
    }
}