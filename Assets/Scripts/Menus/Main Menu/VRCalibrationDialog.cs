using System.Collections;
using Core;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Menus.Main_Menu {
    public class VRCalibrationDialog : MenuBase {
        [SerializeField] private TopMenu topMenu;
        [SerializeField] private NewPlayerWelcomeDialog newPlayerWelcomeDialog;
        [SerializeField] private ProfileMenu profileMenu;
        public bool showWelcomeDialogNext;

        private void OnEnable() {
            IEnumerator CalibrateAfterFirstLoad() {
                yield return new WaitForEndOfFrame();
                OnCalibrate();
            }

            StartCoroutine(CalibrateAfterFirstLoad());
        }

        public void OnCalibrate() {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin) Game.Instance.ResetHmdView(xrOrigin, xrOrigin.transform.parent);
        }

        public void OnAccept() {
            if (showWelcomeDialogNext)
                newPlayerWelcomeDialog.Open(profileMenu);
            else
                topMenu.Open(null);

            Hide();
        }
    }
}