using Audio;
using Core;
using Menus.Pause_Menu;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class MultiPlayerMenu : MenuBase {

        [SerializeField] private TopMenu topMenu;
        [SerializeField] private LobbyMenu lobbyMenu;
        [SerializeField] private JoinMenu joinMenu;
        
        public void ClosePanel() {
            Cancel();
        }

        public void OpenHostPanel() {
            Game.Instance.SessionStatus = SessionStatus.LobbyMenu;
            Progress(lobbyMenu);
            lobbyMenu.StartHost();
        }

        public void OpenJoinPanel() {
            Progress(joinMenu);
        }
    }
}