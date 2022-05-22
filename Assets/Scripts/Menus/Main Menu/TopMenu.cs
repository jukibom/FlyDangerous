using Core;
using Menus.Options;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Menus.Main_Menu {
    public class TopMenu : MenuBase {
        [SerializeField] private SinglePlayerMenu singlePlayerMenu;
        [SerializeField] private MultiplayerNoticeMenu multiplayerNoticeMenu;
        [SerializeField] private MultiPlayerMenu multiPlayerMenu;
        [SerializeField] private ServerBrowserMenu serverBrowserMenu;
        [SerializeField] private ProfileMenu profileMenu;
        [SerializeField] private OptionsMenu optionsMenu;
        [SerializeField] private PatchNotesMenu patchNotesMenu;

        [SerializeField] private GameObject patchNotesUpdatedText;

        public void Start() {
            // needed on game start
            if (defaultActiveButton != null) defaultActiveButton.Select();
        }

        public void SetPatchNotesUpdated(bool isUpdated) {
            patchNotesUpdatedText.SetActive(isUpdated);
        }

        public void OpenSinglePlayerPanel() {
            NetworkServer.dontListen = true;
            FdNetworkManager.Instance.StartHost();
            Game.Instance.SessionStatus = SessionStatus.SinglePlayerMenu;

            Progress(singlePlayerMenu);
        }

        public void OpenMultiPlayerMenu() {
            Progress(multiplayerNoticeMenu);
        }

        public void OpenProfileMenu() {
            Progress(profileMenu);
            profileMenu.SetCancelButtonEnabled(true);
        }

        public void OpenOptionsPanel() {
            Progress(optionsMenu);
        }

        public void CloseOptionsPanel() {
            optionsMenu.Hide();
            Show();
        }

        public void OpenPatchNotes() {
            SetPatchNotesUpdated(false);
            Progress(patchNotesMenu);
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