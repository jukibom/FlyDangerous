using Audio;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class MenuBase : MonoBehaviour, ICancelHandler {
        private static readonly int open = Animator.StringToHash("Open");

        [SerializeField] protected Button defaultActiveButton;
        protected Animator animator;
        protected MenuBase caller;

        protected void Awake() {
            animator = GetComponent<Animator>();
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
        public void Progress(MenuBase nextMenu, bool setCallChain = true, bool playDialogOpenSound = true) {
            if (playDialogOpenSound)
                PlayOpenSound();
            else
                PlayApplySound();
            nextMenu.Open(setCallChain ? this : null);
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
            animator.SetBool(open, true);
            defaultActiveButton.Select();
        }

        protected void PlayOpenSound() {
            UIAudioManager.Instance.Play("ui-dialog-open");
        }

        protected void PlayApplySound() {
            UIAudioManager.Instance.Play("ui-confirm");
        }

        protected void PlayCancelSound() {
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