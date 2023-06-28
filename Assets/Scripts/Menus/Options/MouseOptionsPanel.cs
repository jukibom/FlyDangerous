using System.Collections.Generic;
using FdUI;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Options {
    public class MouseOptionsPanel : MonoBehaviour {
        [SerializeField] private Dropdown mouseInputDropdown;
        [SerializeField] private Checkbox mouseRelativeForceCheckbox;
        [SerializeField] private Checkbox mouseRelativeSeparateSensitivityCheckbox;

        [SerializeField] private List<GameObject> relativeMouseOptions;
        [SerializeField] private List<GameObject> disabledRelativeMouseOptions;
        [SerializeField] private List<GameObject> relativeMouseSensitivityOptions;
        [SerializeField] private List<GameObject> disabledRelativeMouseSensitivityOptions;

        private void OnEnable() {
            OnOptionChange();
        }

        public void OnOptionChange() {
            var showRelativeOptions = mouseInputDropdown.value == 0 || mouseRelativeForceCheckbox.isChecked;
            var showSeparateSensitivityOptions =
                showRelativeOptions && mouseRelativeSeparateSensitivityCheckbox.isChecked;

            foreach (var relativeMouseOption in relativeMouseOptions)
                relativeMouseOption.SetActive(showRelativeOptions);

            foreach (var disabledRelativeMouseOption in disabledRelativeMouseOptions)
                disabledRelativeMouseOption.SetActive(!showRelativeOptions);

            foreach (var relativeMouseSensitivityOption in relativeMouseSensitivityOptions)
                relativeMouseSensitivityOption.SetActive(showSeparateSensitivityOptions);

            foreach (var disabledRelativeMouseSensitivityOption in disabledRelativeMouseSensitivityOptions)
                disabledRelativeMouseSensitivityOption.SetActive(!showSeparateSensitivityOptions);
        }
    }
}