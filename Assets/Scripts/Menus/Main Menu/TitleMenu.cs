using System.Collections;
using Core;
using Misc;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class TitleMenu : MenuBase {
        [SerializeField] private Image pressAnyButton;
        [SerializeField] private InputSystemUIInputModule inputHandler;
        [SerializeField] private Image cursor;

        public MenuBase nextMenu;

        private bool _quitting;

        public void Update() {
            // super complex advanced flashing sprite code
            pressAnyButton.enabled = Time.time % 0.8f > 0.4f;
        }

        public void OnEnable() {
            AddOnAnyKeyHandler();
            cursor.enabled = false;
        }

        public void OnDisable() {
            cursor.enabled = true;
        }

        public void Quit() {
            PlayCancelSound();
            _quitting = true;
            Game.Instance.QuitGame();
        }

        public override void OnCancel(BaseEventData eventData) {
            Quit();
        }

        #region async pain

        // wait SOME (why? no idea don't care) frames to register even such that back button from top level doesn't also trigger forward
        private void AddOnAnyKeyHandler() {
            IEnumerator AddHandler() {
                yield return YieldExtensions.WaitForFixedFrames(25);
                InputSystem.onAnyButtonPress
                    // literally just existing in VR counts as a button press, apparently
                    .Where(e => !e.displayName.Equals("userpresence"))
                    .CallOnce(e => { AnyKeyPressed(); });
            }

            StartCoroutine(AddHandler());
        }

        // stop the stupid bloody input handler from using menu back (quit) as next panel
        private void AnyKeyPressed() {
            IEnumerator AnyKeyProgress() {
                yield return new WaitForFixedUpdate();
                inputHandler.enabled = false;
                yield return new WaitForFixedUpdate();
                // ReSharper disable once Unity.InefficientPropertyAccess
                inputHandler.enabled = true;
                if (!_quitting) Progress(nextMenu, false);
            }

            StartCoroutine(AnyKeyProgress());
        }

        #endregion
    }
}