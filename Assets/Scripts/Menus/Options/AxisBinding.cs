using FdUI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menus.Options {
    public enum AxisBindingOrientation {
        UpDown,
        LeftRight
    }

    /**
     * Helper class for assigning axis bindings in a generalised set of components
     * (full range, individual, invert / deadzone components etc)
     */
    public class AxisBinding : MonoBehaviour {
        [Header("Axis Attributes")] [Tooltip("Sets the text label on all child elements")] [SerializeField]
        private string axisBindingLabel;

        [Tooltip("Determines what language to use in labels")] [SerializeField]
        private AxisBindingOrientation axisBindingOrientation;

        [Header("Axis Action Reference")]
        [Tooltip(
            "This action ref should have three groups of primary and secondary bindings - one for full range (index 0 and 1), one for positive axis (3 and 6) and one for negative axis (4 and 7)")]
        [SerializeField]
        private InputActionReference action;

        [Header("Rebind Overlay Components")] [Tooltip("The component to show while binding action is in progress")] [SerializeField]
        private GameObject rebindOverlay;

        [Tooltip("The text value to manipulate while binding action is in progress")] [SerializeField]
        private TMPro.TMP_Text rebindText;

        [Header("Internal Components - Don't touch")] [SerializeField]
        private TMPro.TMP_Text fullAxisLabel;

        [SerializeField] private TMPro.TMP_Text fullAxisInvertLabel;
        [SerializeField] private TMPro.TMP_Text fullAxisDeadzoneLabel;
        [SerializeField] private TMPro.TMP_Text positiveAxisLabel;
        [SerializeField] private TMPro.TMP_Text negativeAxisLabel;

        [SerializeField] private RebindAction fullAxisRebindActionComponent;
        [SerializeField] private RebindAction positiveAxisRebindActionComponent;
        [SerializeField] private RebindAction negativeAxisRebindActionComponent;

        [SerializeField] private FdSlider primaryAxisDeadzoneSlider;
        [SerializeField] private FdSlider secondaryAxisDeadzoneSlider;

        private void OnEnable() {
            primaryAxisDeadzoneSlider.onValueChanged.AddListener(fullAxisRebindActionComponent.SetPrimaryAxisDeadzone);
            secondaryAxisDeadzoneSlider.onValueChanged.AddListener(fullAxisRebindActionComponent.SetSecondaryAxisDeadzone);
        }

        private void OnDisable() {
            primaryAxisDeadzoneSlider.onValueChanged.RemoveListener(fullAxisRebindActionComponent.SetPrimaryAxisDeadzone);
            secondaryAxisDeadzoneSlider.onValueChanged.RemoveListener(fullAxisRebindActionComponent.SetSecondaryAxisDeadzone);
        }

        private void OnValidate() {
            if (action == null) return;
            fullAxisLabel.text = $"{axisBindingLabel} Full Axis";
            fullAxisInvertLabel.text = $"Invert {axisBindingLabel} Axis";
            fullAxisDeadzoneLabel.text = $"{axisBindingLabel} Axis Deadzone";
            positiveAxisLabel.text = $"{axisBindingLabel} {(axisBindingOrientation == AxisBindingOrientation.UpDown ? "Up" : "Right")}";
            negativeAxisLabel.text = $"{axisBindingLabel} {(axisBindingOrientation == AxisBindingOrientation.UpDown ? "Down" : "Left")}";

            fullAxisRebindActionComponent.actionReference = action;
            positiveAxisRebindActionComponent.actionReference = action;
            negativeAxisRebindActionComponent.actionReference = action;

            fullAxisRebindActionComponent.PrimaryBindingIndex = 0;
            fullAxisRebindActionComponent.SecondaryBindingIndex = 1;

            positiveAxisRebindActionComponent.PrimaryBindingIndex = 3;
            positiveAxisRebindActionComponent.SecondaryBindingIndex = 6;

            negativeAxisRebindActionComponent.PrimaryBindingIndex = 4;
            negativeAxisRebindActionComponent.SecondaryBindingIndex = 7;

            fullAxisRebindActionComponent.rebindOverlay = rebindOverlay;
            negativeAxisRebindActionComponent.rebindOverlay = rebindOverlay;
            positiveAxisRebindActionComponent.rebindOverlay = rebindOverlay;

            fullAxisRebindActionComponent.rebindText = rebindText;
            negativeAxisRebindActionComponent.rebindText = rebindText;
            positiveAxisRebindActionComponent.rebindText = rebindText;
        }
    }
}