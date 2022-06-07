using System.Collections;
using Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

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
            var xrRig = FindObjectOfType<XRRig>();
            if (xrRig) Game.Instance.ResetHmdView(xrRig, xrRig.transform.parent);
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