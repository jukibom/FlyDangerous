using Core;
using UnityEngine;

namespace Menus.Main_Menu {
    public class SinglePlayerMenu : MenuBase {
        [SerializeField] private TimeTrialMenu timeTrialMenu;
        [SerializeField] private FreeRoamMenu freeRoamMenu;
        [SerializeField] private LoadCustomMenu loadCustomMenu;

        protected override void OnClose() {
            Game.Instance.SessionStatus = SessionStatus.Offline;
            FdNetworkManager.Instance.StopAll();
        }

        public void ClosePanel() {
            Cancel();
        }

        public void OpenCampaignPanel() {
            Debug.Log("Nope not yet!");
        }

        public void OpenTimeTrialPanel() {
            Progress(timeTrialMenu);
        }

        public void OpenFreeRoamPanel() {
            Progress(freeRoamMenu);
        }

        public void OpenLoadCustomPanel() {
            Progress(loadCustomMenu);
        }
    }
}