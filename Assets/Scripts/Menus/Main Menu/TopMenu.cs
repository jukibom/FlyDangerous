using Core;
using Menus.Options;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Menus.Main_Menu {
    public class TopMenu : MenuBase {
        [SerializeField] private SinglePlayerMenu singlePlayerMenu;
        [SerializeField] private MultiPlayerMenu multiPlayerMenu;
        [SerializeField] private ServerBrowserMenu serverBrowserMenu;
        [SerializeField] private ProfileMenu profileMenu;
        [SerializeField] private OptionsMenu optionsMenu;

        public void Start() {
            // needed on game start
            defaultActiveButton.Select();
        }

        public void OpenSinglePlayerPanel() {
            NetworkServer.dontListen = true;
            FdNetworkManager.Instance.StartHost();
            Game.Instance.SessionStatus = SessionStatus.SinglePlayerMenu;

            Progress(singlePlayerMenu);
        }

        public void OpenMultiPlayerMenu() {
            if (FdNetworkManager.Instance.HasMultiplayerServices) // we have some online services hooked up, load the game browser
                Progress(serverBrowserMenu);
            else // revert to old-school
                Progress(multiPlayerMenu);
        }

        public void OpenProfileMenu() {
            Progress(profileMenu);
        }

        public void OpenOptionsPanel() {
            Progress(optionsMenu);
        }

        public void CloseOptionsPanel() {
            optionsMenu.Hide();
            Show();
        }

        public void OpenDiscordLink() {
            PlayOpenSound();
            Application.OpenURL("https://discord.gg/4daSEUKZ6A");
        }

        public void Quit() {
            PlayCancelSound();
            Game.Instance.QuitGame();
        }

        public override void OnCancel(BaseEventData eventData) {
            Quit();
        }
    }
}