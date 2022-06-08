#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

////TODO: support multi-object editing

namespace Menus.Options {
    /// <summary>
    ///     A custom inspector for <see cref="RebindActionUI" /> which provides a more convenient way for
    ///     picking the binding which to rebind.
    /// </summary>
    [CustomEditor(typeof(RebindAction))]
    public class RebindActionEditor : Editor {
        private readonly GUIContent m_DisplayOptionsLabel = new("Display Options");
        private readonly GUIContent m_EventsLabel = new("Events");

        private readonly GUIContent m_PrimaryBindingLabel = new("Primary Binding");
        private readonly GUIContent m_SecondaryBindingLabel = new("Secondary Binding");
        private readonly GUIContent m_UILabel = new("UI");
        private SerializedProperty m_ActionLabelProperty;

        private SerializedProperty m_ActionProperty;
        private SerializedProperty m_ActionProtectedProperty;
        private GUIContent[] m_BindingOptions;
        private string[] m_BindingOptionValues;
        private SerializedProperty m_DisplayStringOptionsProperty;
        private SerializedProperty m_PrimaryBindingButton;
        private SerializedProperty m_PrimaryBindingIdProperty;
        private SerializedProperty m_PrimaryBindingTextProperty;
        private SerializedProperty m_RebindOverlayProperty;
        private SerializedProperty m_RebindStartEventProperty;
        private SerializedProperty m_RebindStopEventProperty;
        private SerializedProperty m_RebindTextProperty;
        private SerializedProperty m_SecondaryBindingIdProperty;
        private SerializedProperty m_SecondaryBindingTextProperty;
        private int m_SelectedPrimaryBindingOption;
        private int m_SelectedSecondaryBindingOption;
        private SerializedProperty m_UpdateBindingUIEventProperty;

        protected void OnEnable() {
            m_ActionProperty = serializedObject.FindProperty("m_Action");
            m_PrimaryBindingIdProperty = serializedObject.FindProperty("m_PrimaryBindingId");
            m_SecondaryBindingIdProperty = serializedObject.FindProperty("m_SecondaryBindingId");
            m_ActionLabelProperty = serializedObject.FindProperty("m_ActionLabel");
            m_PrimaryBindingButton = serializedObject.FindProperty("m_PrimaryBindingButton");
            m_PrimaryBindingTextProperty = serializedObject.FindProperty("m_PrimaryBindingText");
            m_SecondaryBindingTextProperty = serializedObject.FindProperty("m_SecondaryBindingText");
            m_ActionProtectedProperty = serializedObject.FindProperty("m_Protected");
            m_RebindOverlayProperty = serializedObject.FindProperty("m_RebindOverlay");
            m_RebindTextProperty = serializedObject.FindProperty("m_RebindText");
            m_UpdateBindingUIEventProperty = serializedObject.FindProperty("m_UpdateBindingUIEvent");
            m_RebindStartEventProperty = serializedObject.FindProperty("m_RebindStartEvent");
            m_RebindStopEventProperty = serializedObject.FindProperty("m_RebindStopEvent");
            m_DisplayStringOptionsProperty = serializedObject.FindProperty("m_DisplayStringOptions");

            RefreshBindingOptions();
        }

        public override void OnInspectorGUI() {
            EditorGUI.BeginChangeCheck();

            // Binding section.
            EditorGUILayout.LabelField(m_PrimaryBindingLabel, Styles.boldLabel);
            using (new EditorGUI.IndentLevelScope()) {
                EditorGUILayout.PropertyField(m_ActionProperty);

                var newSelectedPrimaryBinding = EditorGUILayout.Popup(m_PrimaryBindingLabel, m_SelectedPrimaryBindingOption, m_BindingOptions);
                if (newSelectedPrimaryBinding != m_SelectedPrimaryBindingOption) {
                    var bindingId = m_BindingOptionValues[newSelectedPrimaryBinding];
                    m_PrimaryBindingIdProperty.stringValue = bindingId;
                    m_SelectedPrimaryBindingOption = newSelectedPrimaryBinding;
                }

                var newSelectedSecondaryBinding = EditorGUILayout.Popup(m_SecondaryBindingLabel, m_SelectedSecondaryBindingOption, m_BindingOptions);
                if (newSelectedSecondaryBinding != m_SelectedSecondaryBindingOption) {
                    var bindingId = m_BindingOptionValues[newSelectedSecondaryBinding];
                    m_SecondaryBindingIdProperty.stringValue = bindingId;
                    m_SelectedSecondaryBindingOption = newSelectedSecondaryBinding;
                }

                var optionsOld = (InputBinding.DisplayStringOptions)m_DisplayStringOptionsProperty.intValue;
                var optionsNew = (InputBinding.DisplayStringOptions)EditorGUILayout.EnumFlagsField(m_DisplayOptionsLabel, optionsOld);

                if (optionsOld != optionsNew)
                    m_DisplayStringOptionsProperty.intValue = (int)optionsNew;

                EditorGUILayout.PropertyField(m_ActionProtectedProperty);
            }

            // UI section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(m_UILabel, Styles.boldLabel);
            using (new EditorGUI.IndentLevelScope()) {
                EditorGUILayout.PropertyField(m_ActionLabelProperty);
                EditorGUILayout.PropertyField(m_PrimaryBindingButton);
                EditorGUILayout.PropertyField(m_PrimaryBindingTextProperty);
                EditorGUILayout.PropertyField(m_SecondaryBindingTextProperty);
                EditorGUILayout.PropertyField(m_RebindOverlayProperty);
                EditorGUILayout.PropertyField(m_RebindTextProperty);
            }

            // Events section.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(m_EventsLabel, Styles.boldLabel);
            using (new EditorGUI.IndentLevelScope()) {
                EditorGUILayout.PropertyField(m_RebindStartEventProperty);
                EditorGUILayout.PropertyField(m_RebindStopEventProperty);
                EditorGUILayout.PropertyField(m_UpdateBindingUIEventProperty);
            }

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                RefreshBindingOptions();
            }
        }

        protected void RefreshBindingOptions() {
            var actionReference = (InputActionReference)m_ActionProperty.objectReferenceValue;
            var action = actionReference?.action;

            if (action == null) {
                m_BindingOptions = new GUIContent[0];
                m_BindingOptionValues = new string[0];
                m_SelectedPrimaryBindingOption = -1;
                m_SelectedSecondaryBindingOption = -1;
                return;
            }

            var bindings = action.bindings;
            var bindingCount = bindings.Count;

            m_BindingOptions = new GUIContent[bindingCount];
            m_BindingOptionValues = new string[bindingCount];
            m_SelectedPrimaryBindingOption = -1;
            m_SelectedSecondaryBindingOption = -1;

            var primaryBindingId = m_PrimaryBindingIdProperty.stringValue;
            var secondaryBindingId = m_SecondaryBindingIdProperty.stringValue;
            for (var i = 0; i < bindingCount; ++i) {
                var binding = bindings[i];
                var bindingId = binding.id.ToString();
                var haveBindingGroups = !string.IsNullOrEmpty(binding.groups);

                // If we don't have a binding groups (control schemes), show the device that if there are, for example,
                // there are two bindings with the display string "A", the user can see that one is for the keyboard
                // and the other for the gamepad.
                var displayOptions =
                    InputBinding.DisplayStringOptions.DontUseShortDisplayNames | InputBinding.DisplayStringOptions.IgnoreBindingOverrides;
                if (!haveBindingGroups)
                    displayOptions |= InputBinding.DisplayStringOptions.DontOmitDevice;

                // Create display string.
                m_BindingOptions[i] = new GUIContent("Global Action (rebind not supported");
                try {
                    var displayString = action.GetBindingDisplayString(i, displayOptions);

                    // Prevent duplicates being omitted (for primary and secondary unbound actions)
                    displayString = $"{i.ToString()}: {displayString}";

                    // If binding is part of a composite, include the part name.
                    if (binding.isPartOfComposite)
                        displayString = $"{ObjectNames.NicifyVariableName(binding.name)}: {displayString}";

                    // Some composites use '/' as a separator. When used in popup, this will lead to to submenus. Prevent
                    // by instead using a backlash.
                    displayString = displayString.Replace('/', '\\');

                    // If the binding is part of control schemes, mention them.
                    if (haveBindingGroups) {
                        var asset = action.actionMap?.asset;
                        if (asset != null) {
                            var controlSchemes = string.Join(", ",
                                binding.groups.Split(InputBinding.Separator)
                                    .Select(x => asset.controlSchemes.FirstOrDefault(c => c.bindingGroup == x).name));

                            displayString = $"{displayString} | {controlSchemes}";
                        }
                    }

                    m_BindingOptions[i].text = displayString;
                    m_BindingOptionValues[i] = bindingId;

                    if (primaryBindingId == bindingId)
                        m_SelectedPrimaryBindingOption = i;

                    if (secondaryBindingId == bindingId)
                        m_SelectedSecondaryBindingOption = i;
                }
                // Doesn't support global types (e.g. `./{Submit}`), so we just move on. 
                catch {
                }
            }
        }

        private static class Styles {
            public static readonly GUIStyle boldLabel = new("MiniBoldLabel");
        }
    }
}
#endif