using Audio;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class MenuBase : MonoBehaviour, ICancelHandler {
        private static readonly int open = Animator.StringToHash("Open");

        [CanBeNull] [SerializeField] protected Button defaultActiveButton;
        protected Animator animator;
        protected MenuBase caller;

        protected Animator Animator {
            get {
                if (animator == null) animator = GetComponent<Animator>();
                return animator;
            }
        }

        // Event from user input, may be overridden 
        public virtual void OnCancel(BaseEventData eventData) {
            Cancel();
        }

        // Animate open the dialog, store the caller for cancel operations and trigger events
        public void Open([CanBeNull] MenuBase withCaller) {
            if (withCaller != null) caller = withCaller;
            Show();
            OnOpen();
        }

        // Progress to a new dialog and set the call as this instance and trigger events
        public void Progress(MenuBase nextMenu, bool setCallChain = true, bool playDialogOpenSound = true, MenuBase withCaller = null) {
            if (playDialogOpenSound)
                PlayOpenSound();
            else
                PlayApplySound();
            nextMenu.Open(setCallChain
                ? withCaller ? withCaller : this
                : null);
            Hide();
            OnProgress();
        }

        // Close the dialog and re-open the calling dialog and trigger events
        public void Cancel() {
            if (caller != null) caller.Open(null);
            PlayCancelSound();
            Hide();
            OnClose();
        }

        // Just hide the dialog / menu: no sound or events raised
        public void Hide() {
            gameObject.SetActive(false);
        }

        // Show the dialog via animation and activate the default button
        protected void Show() {
            gameObject.SetActive(true);
            Animator.SetBool(open, true);
            if (defaultActiveButton != null) defaultActiveButton.Select();
        }

        public void PlayOpenSound() {
            UIAudioManager.Instance.Play("ui-dialog-open");
        }

        public void PlayApplySound() {
            UIAudioManager.Instance.Play("ui-confirm");
        }

        public void PlayCancelSound() {
            UIAudioManager.Instance.Play("ui-cancel");
        }

        protected virtual void OnOpen() {
        }

        protected virtual void OnProgress() {
        }

        protected virtual void OnClose() {
        }
    }
}