using Core;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class JoinMenu : MenuBase {
        [SerializeField] private LobbyMenu lobbyMenu;
        [SerializeField] private ConnectingDialog connectingDialog;

        [Header("Input Fields")] [SerializeField]
        private InputField playerName;

        [SerializeField] private InputField serverIPAddress;
        [SerializeField] private InputField serverPort;
        [SerializeField] private InputField serverPassword;

        private void OnEnable() {
            playerName.interactable = !Player.IsUsingOnlineName;
            playerName.text = Player.LocalPlayerName;
            serverIPAddress.text = Preferences.Instance.GetString("lastUsedServerJoinAddress");
            serverPort.text = Preferences.Instance.GetString("lastUsedServerJoinPort");
            serverPassword.text = Preferences.Instance.GetString("lastUsedServerPassword");
        }

        protected override void OnClose() {
            Game.Instance.SessionStatus = SessionStatus.Offline;
            FdNetworkManager.Instance.StopAll();
        }

        public void OnTextEntryChange() {
            // TODO: Input validation on player name?
            Preferences.Instance.SetString("playerName", playerName.text);
            Preferences.Instance.SetString("lastUsedServerJoinAddress", serverIPAddress.text);
            Preferences.Instance.SetString("lastUsedServerJoinPort", serverPort.text);
            Preferences.Instance.SetString("lastUsedServerPassword", serverPassword.text);
            Preferences.Instance.Save();
        }

        public void Join() {
            // save entries
            OnTextEntryChange();
            Progress(connectingDialog);

            var hostAddress = serverIPAddress.text.Length > 0 ? serverIPAddress.text : serverIPAddress.placeholder.GetComponent<Text>().text;
            var port = serverPort.text.Length > 0 ? serverPort.text : serverPort.placeholder.GetComponent<Text>().text;
            Debug.Log("Connecting to " + hostAddress + ":" + port);
            connectingDialog.Connect(lobbyMenu, hostAddress, port, serverPassword.text);
        }
    }
}