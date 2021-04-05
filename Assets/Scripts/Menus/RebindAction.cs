using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menus {
    public class RebindAction : MonoBehaviour {

        [Serializable]
        public class UpdateBindingUIEvent : UnityEvent<RebindAction, string, string, string> {
        }

        [Serializable]
        public class
            InteractiveRebindEvent : UnityEvent<RebindAction, InputActionRebindingExtensions.RebindingOperation> {
        }

        [SerializeField] private InputActionReference m_Action;
        [SerializeField] private string m_BindingId;
        [SerializeField] private InputBinding.DisplayStringOptions m_DisplayStringOptions;

        [SerializeField] private Text m_ActionLabel;
        [SerializeField] private Text m_BindingText;
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

        public string bindingId {
            get => m_BindingId;
            set {
                m_BindingId = value;
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

        public Text bindingText {
            get => m_BindingText;
            set {
                m_BindingText = value;
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


        public void UpdateActionLabel() {
        }

        public void UpdateBindingDisplay() {
        }
    }
}