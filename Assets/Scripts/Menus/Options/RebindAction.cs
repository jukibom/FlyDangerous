using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Misc;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menus.Options {

    public enum BindingType {
        Primary,
        Secondary
    }
    
    public class RebindAction : MonoBehaviour {

        [Serializable]
        public class UpdateBindingUIEvent : UnityEvent<RebindAction, string, string, string> {
        }

        [Serializable]
        public class
            InteractiveRebindEvent : UnityEvent<RebindAction, InputActionRebindingExtensions.RebindingOperation> {
        }

        [SerializeField] private InputActionReference m_Action;
        [SerializeField] private string m_PrimaryBindingId;
        [SerializeField] private string m_SecondaryBindingId;
        [SerializeField] private InputBinding.DisplayStringOptions m_DisplayStringOptions;

        [SerializeField] private Text m_ActionLabel;
        [SerializeField] private Button m_PrimaryBindingButton;
        [SerializeField] private Text m_PrimaryBindingText;
        [SerializeField] private Text m_SecondaryBindingText;
        [SerializeField] private Boolean m_Protected;
        [SerializeField] private GameObject m_RebindOverlay;
        [SerializeField] private Text m_RebindText;
        [SerializeField] private UpdateBindingUIEvent m_UpdateBindingUIEvent;
        [SerializeField] private InteractiveRebindEvent m_RebindStartEvent;
        [SerializeField] private InteractiveRebindEvent m_RebindStopEvent;

        private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;
        private static List<RebindAction> s_RebindActions;

        public InputActionReference actionReference {
            get => m_Action;
            set {
                m_Action = value;
                UpdateActionLabel();
                UpdateBindingDisplay();
            }
        }

        public string primaryBbindingId {
            get => m_PrimaryBindingId;
            set {
                m_PrimaryBindingId = value;
                UpdateBindingDisplay();
            }
        }

        public string secondaryBinding {
            get => m_SecondaryBindingId;
            set {
                m_SecondaryBindingId = value;
                UpdateBindingDisplay();
            }
        }

        public InputBinding.DisplayStringOptions displayStringOptions {
            get => m_DisplayStringOptions;
            set {
                m_DisplayStringOptions = value;
                UpdateBindingDisplay();
            }
        }

        public Text actionLabel {
            get => m_ActionLabel;
            set {
                m_ActionLabel = value;
                UpdateActionLabel();
            }
        }

        public Text primaryBindingText {
            get => m_PrimaryBindingText;
            set {
                m_PrimaryBindingText = value;
                UpdateBindingDisplay();
            }
        }
        public Text secondaryBindingText {
            get => m_SecondaryBindingText;
            set {
                m_SecondaryBindingText = value;
                UpdateBindingDisplay();
            }
        }

        public Text rebindPrompt {
            get => m_RebindText;
            set => m_RebindText = value;
        }

        public GameObject rebindOverlay {
            get => m_RebindOverlay;
            set => m_RebindOverlay = value;
        }

        public UpdateBindingUIEvent updateBindingUIEvent {
            get {
                if (m_UpdateBindingUIEvent == null)
                    m_UpdateBindingUIEvent = new UpdateBindingUIEvent();
                return m_UpdateBindingUIEvent;
            }
        }

        public InteractiveRebindEvent startRebindEvent {
            get {
                if (m_RebindStartEvent == null)
                    m_RebindStartEvent = new InteractiveRebindEvent();
                return m_RebindStartEvent;
            }
        }

        public InteractiveRebindEvent stopRebindEvent {
            get {
                if (m_RebindStopEvent == null)
                    m_RebindStopEvent = new InteractiveRebindEvent();
                return m_RebindStopEvent;
            }
        }

        public InputActionRebindingExtensions.RebindingOperation ongoingRebind => m_RebindOperation;

        public bool ResolveActionAndBinding(string bindingId, out InputAction action, out int bindingIndex) {
            bindingIndex = -1;

            action = m_Action?.action;
            if (action == null)
                return false;

            if (string.IsNullOrEmpty(bindingId))
                return false;

            // Look up binding index.
            var bindingIdGuid = new Guid(bindingId);
            bindingIndex = action.bindings.IndexOf(x => x.id == bindingIdGuid);
            if (bindingIndex == -1)
            {
                Debug.LogError($"Cannot find binding with ID '{bindingIdGuid}' on '{action}'", this);
                return false;
            }

            return true;
        }

        public void UpdateBindingDisplay() {
            UpdatePrimaryBindingDisplay();
            UpdateSecondaryBindingDisplay();
        } 
        
        public void ResetToDefault() {
            ResetPrimaryBinding();
            ResetSecondaryBinding();
            UpdateBindingDisplay();
        }

        public void ToggleInverseAxisPrimary() {
            if (!ResolveActionAndBinding(m_PrimaryBindingId, out var action, out var bindingIndex))
                return;
            ToggleInverseAxis(action, bindingIndex);
        }
        
        public void ToggleInverseAxisSecondary() {
            if (!ResolveActionAndBinding(m_SecondaryBindingId, out var action, out var bindingIndex))
                return;
            ToggleInverseAxis(action, bindingIndex);
        }

        public void SetPrimaryAxisDeadzone(float deadzone) {
            if (!ResolveActionAndBinding(m_PrimaryBindingId, out var action, out var bindingIndex))
                return;
            SetDeadzone(action, bindingIndex, deadzone);
        }

        public void SetSecondaryAxisDeadzone(float deadzone) {
            if (!ResolveActionAndBinding(m_SecondaryBindingId, out var action, out var bindingIndex))
                return;
            SetDeadzone(action, bindingIndex, deadzone);
        }

        private void ToggleInverseAxis(InputAction action, int bindingIndex) {
            var binding = action.bindings[bindingIndex];
            binding.overrideProcessors = MakeAxisProcessorString(!IsInverseEnabled(binding), GetAxisDeadzone(binding));
            action.ChangeBinding(bindingIndex).To(binding);
        }

        private bool IsInverseEnabled(InputBinding binding) {
            return (binding.overrideProcessors != null && binding.overrideProcessors.Contains("Invert"));
        }

        private void SetDeadzone(InputAction action, int bindingIndex, float deadzone) {
            deadzone = MathfExtensions.Clamp(0, 1, deadzone);
            var binding = action.bindings[bindingIndex];

            binding.overrideProcessors = MakeAxisProcessorString(IsInverseEnabled(binding), deadzone);
            action.ChangeBinding(bindingIndex).To(binding);
        }

        private float GetAxisDeadzone(InputBinding binding) {
            if (binding.overrideProcessors != null && binding.overrideProcessors.Contains("AxisDeadzone")) {
                // AxisDeadzone(min=0.05,max=0.99)
                var match = Regex.Match(binding.overrideProcessors,@"AxisDeadzone\(min\s*=\s*(\d*\.?\d*)");
                if (match.Success) {
                    try {
                        return float.Parse(match.Result("$1"));
                    }
                    catch {
                        // ignored
                        Debug.LogWarning($"Failed to parse axis deadzone for {binding.name} ({match.Value}).");
                        return 0;
                    }
                }
            }
            return 0;
        }

        private string MakeAxisProcessorString(bool invert, float deadzone) {
            return invert
                ? $"Invert,AxisDeadzone(min={deadzone},max=1.00)"
                : $"AxisDeadzone(min={deadzone},max=1.00)";
        }

        public void StartInteractivePrimaryRebind() {
            if (!ResolveActionAndBinding(m_PrimaryBindingId, out var action, out var bindingIndex))
                return;

            // If the binding is a composite, we need to rebind each part in turn.
            if (action.bindings[bindingIndex].isComposite)
            {
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                    PerformInteractiveRebind(m_PrimaryBindingText, action, firstPartIndex, BindingType.Primary, allCompositeParts: true);
            }
            else
            {
                PerformInteractiveRebind(m_PrimaryBindingText, action, bindingIndex, BindingType.Primary);
            }
        }
        
        public void StartInteractiveSecondaryRebind() {
            if (!ResolveActionAndBinding(m_SecondaryBindingId, out var action, out var bindingIndex))
                return;

            // If the binding is a composite, we need to rebind each part in turn.
            if (action.bindings[bindingIndex].isComposite)
            {
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                    PerformInteractiveRebind(m_SecondaryBindingText, action, firstPartIndex, BindingType.Secondary, allCompositeParts: true);
            }
            else
            {
                PerformInteractiveRebind(m_SecondaryBindingText, action, bindingIndex, BindingType.Secondary);
            }
        }

        private void PerformInteractiveRebind(Text bindingText, InputAction action, int bindingIndex, BindingType bindingType, bool allCompositeParts = false) {
            m_RebindOperation?.Cancel(); // Will null out m_RebindOperation.

            Boolean shouldReEnable = action.enabled;
            action.Disable();
            
            void CleanUp()
            {
                m_RebindOperation?.Dispose();
                m_RebindOperation = null;
                if (shouldReEnable) {
                    action.Enable();
                }
            }

            // Configure the rebind.
            m_RebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .WithoutGeneralizingPathOfSelectedControl()
                .OnPotentialMatch(operation => {
                    // special case for delete key - unbind the binding!
                    if (operation.selectedControl.path == "/Keyboard/delete") {
                        var binding = operation.action.bindings[bindingIndex];
                        binding.overridePath = "";
                        operation.action.ChangeBinding(bindingIndex).To(binding);
                        operation.Cancel();
                        return;
                    }

                    // special case for Axis binds - we want to ignore button presses on axis binds (but not
                    // necessarily axis binds on other button bindings). 
                    // NOTE: This is not a direct comparison because "Button" bindings receive input "Key" from
                    // the keyboard. No, I am not making this shit up I swear
                    if (m_RebindOperation.expectedControlType == "Axis" && operation.selectedControl.layout != "Axis") {
                        operation.Cancel();
                    }
                    else {
                        operation.Complete();
                    }
                })
                .OnComplete(
                    operation => {
                        m_RebindOverlay?.SetActive(false);
                        m_RebindStopEvent?.Invoke(this, operation);
                        UpdateBindingDisplay();
                        CleanUp();

                        // If there's more composite parts we should bind, initiate a rebind
                        // for the next part.
                        if (allCompositeParts) {
                            var nextBindingIndex = bindingIndex + 1;
                            if (nextBindingIndex < action.bindings.Count &&
                                action.bindings[nextBindingIndex].isPartOfComposite)
                                PerformInteractiveRebind(bindingText, action, nextBindingIndex, bindingType, true);
                        }
                    })
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(
                    operation => {
                        m_RebindStopEvent?.Invoke(this, operation);
                        m_RebindOverlay?.SetActive(false);
                        UpdateBindingDisplay();
                        CleanUp();
                    });

            // If it's a part binding, show the name of the part in the UI.
            var partName = $"{action.name}";
            if (action.bindings[bindingIndex].isPartOfComposite)
                partName = $"Binding '{action.name} : {action.bindings[bindingIndex].name}'. ";

            // Bring up rebind overlay, if we have one.
            m_RebindOverlay?.SetActive(true);
            if (m_RebindText != null) {
                var bindingTypeName = bindingType == BindingType.Primary ? "Primary" : "Secondary";
                var text = !string.IsNullOrEmpty(m_RebindOperation.expectedControlType)
                    ? $"{partName.ToUpper()}:  {bindingTypeName.ToUpper()} BINDING\n\nWaiting for {m_RebindOperation.expectedControlType} input...\n\n-------------------------------------------\n\nESC to cancel\nDEL to unbind"
                    : $"{partName}\nWaiting for input...\n\n-------------------------------------------\n\nESC to cancel\nDEL to unbind";
                m_RebindText.text = text;
            }

            // If we have no rebind overlay and no callback but we have a binding text label,
            // temporarily set the binding text label to "<Waiting>".
            if (m_RebindOverlay == null && m_RebindText == null && m_RebindStartEvent == null && bindingText != null)
                bindingText.text = "<Waiting...>";

            // Give listeners a chance to act on the rebind starting.
            m_RebindStartEvent?.Invoke(this, m_RebindOperation);

            m_RebindOperation.Start();
        }
        
        
        private void UpdatePrimaryBindingDisplay() {
            m_PrimaryBindingButton.interactable = true;

            var displayString = string.Empty;
            var deviceLayoutName = default(string);
            var controlPath = default(string);

            // Get display string from action.
            var action = m_Action?.action;
            if (action != null)
            {
                var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_PrimaryBindingId);
                if (bindingIndex != -1)
                    displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
            }

            // Special protected status
            if (m_Protected) {  
                m_PrimaryBindingButton.interactable = false;
                displayString = "(locked) " + displayString;
            }

            if (displayString.Length == 0) {
                displayString = "(not bound)";
            }
            
            // Set on label (if any).
            if (m_PrimaryBindingText != null)
                m_PrimaryBindingText.text = displayString;

            // Give listeners a chance to configure UI in response.
            m_UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
        }
        
        private void UpdateSecondaryBindingDisplay() {
            var displayString = string.Empty;
            var deviceLayoutName = default(string);
            var controlPath = default(string);

            // Get display string from action.
            var action = m_Action?.action;
            if (action != null)
            {
                var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_SecondaryBindingId);
                if (bindingIndex != -1)
                    displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
            }

            if (displayString.Length == 0) {
                displayString = "(not bound)";
            }
            
            // Set on label (if any).
            if (m_SecondaryBindingText != null)
                m_SecondaryBindingText.text = displayString;

            // Give listeners a chance to configure UI in response.
            m_UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
        }

        private void UpdateAxisOptions() {
            var axisOptions = GetComponent<AxisOptions>();
            if (axisOptions != null) {
                if (ResolveActionAndBinding(m_PrimaryBindingId, out var action, out var bindingIndex)) {
                    axisOptions.primaryInverseCheckbox.isChecked = IsInverseEnabled(action.bindings[bindingIndex]);
                    axisOptions.primaryDeadzoneSlider.Value = GetAxisDeadzone(action.bindings[bindingIndex]);
                }
                if (ResolveActionAndBinding(m_SecondaryBindingId, out action, out bindingIndex)) {
                    axisOptions.secondaryInverseCheckbox.isChecked = IsInverseEnabled(action.bindings[bindingIndex]);
                    axisOptions.secondaryDeadzoneSlider.Value = GetAxisDeadzone(action.bindings[bindingIndex]);
                }
            }
        }
        
        protected void OnEnable() {
            if (s_RebindActions == null)
                s_RebindActions = new List<RebindAction>();
            s_RebindActions.Add(this);
            if (s_RebindActions.Count == 1)
                InputSystem.onActionChange += OnActionChange;
            UpdateBindingDisplay();
            UpdateAxisOptions();
        }

        protected void OnDisable()
        {
            m_RebindOperation?.Dispose();
            m_RebindOperation = null;
            
            s_RebindActions.Remove(this);
            if (s_RebindActions.Count == 0)
            {
                s_RebindActions = null;
                InputSystem.onActionChange -= OnActionChange;
            }
        }
        
        // When the action system re-resolves bindings, we want to update our UI in response. While this will
        // also trigger from changes we made ourselves, it ensures that we react to changes made elsewhere. If
        // the user changes keyboard layout, for example, we will get a BoundControlsChanged notification and
        // will update our UI to reflect the current keyboard layout.
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
                return;

            var action = obj as InputAction;
            var actionMap = action?.actionMap ?? obj as InputActionMap;
            var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

            for (var i = 0; i < s_RebindActions.Count; ++i)
            {
                var component = s_RebindActions[i];
                var referencedAction = component.actionReference?.action;
                if (referencedAction == null)
                    continue;

                if (referencedAction == action ||
                    referencedAction.actionMap == actionMap ||
                    referencedAction.actionMap?.asset == actionAsset)
                    component.UpdateBindingDisplay();
            }
        }
        
        // We want the label for the action name to update in edit mode, too, so
        // we kick that off from here.
        #if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateActionLabel();
            UpdateBindingDisplay();
        }

        #endif

        private void UpdateActionLabel()
        {
            if (m_ActionLabel != null && m_ActionLabel.text == null || m_ActionLabel.text.Length == 0)
            {
                var action = m_Action?.action;
                m_ActionLabel.text = action != null ? action.name : string.Empty;
            }
        }
        
        private void ResetPrimaryBinding() {
            if (!ResolveActionAndBinding(m_PrimaryBindingId, out var primaryAction, out var primaryBindingIndex))
                return;
            
            if (primaryAction.bindings[primaryBindingIndex].isComposite)
            {
                // It's a composite. Remove overrides from part bindings.
                for (var i = primaryBindingIndex + 1; i < primaryAction.bindings.Count && primaryAction.bindings[i].isPartOfComposite; ++i)
                    primaryAction.RemoveBindingOverride(i);
            }
            else
            {
                primaryAction.RemoveBindingOverride(primaryBindingIndex);
            }
        }

        private void ResetSecondaryBinding() {
            if (!ResolveActionAndBinding(m_SecondaryBindingId, out var secondaryAction, out var secondaryBindingIndex))
                return;
            
            if (secondaryAction.bindings[secondaryBindingIndex].isComposite)
            {
                // It's a composite. Remove overrides from part bindings.
                for (var i = secondaryBindingIndex + 1; i < secondaryAction.bindings.Count && secondaryAction.bindings[i].isPartOfComposite; ++i)
                    secondaryAction.RemoveBindingOverride(i);
            }
            else
            {
                secondaryAction.RemoveBindingOverride(secondaryBindingIndex);
            }
        }
    }
}