using Core.MapData;
using Menus.Main_Menu;
using Menus.Main_Menu.Components;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Player {
    public class LobbyPlayer : FdPlayer {
        [SyncVar] public bool isHost;

        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        public string playerName = "Connecting ...";

        [SyncVar(hook = nameof(OnReadyStatusChanged))]
        public bool isReady;

        [SerializeField] private Text playerNameLabel;
        [SerializeField] private InputField playerNameTextEntry;
        [SerializeField] private RawImage readyStatus;

        private LobbyMenu _lobby;

        private LobbyMenu LobbyUI {
            get {
                if (_lobby == null) _lobby = FindObjectOfType<LobbyMenu>();

                return _lobby;
            }
        }

        private void Start() {
            DontDestroyOnLoad(this);
        }

        private void FixedUpdate() {
            if (!transform.parent) AttachToLobbyContainer();
        }

        public void UpdateLobby(LevelData lobbyLevelData, short maxPlayers) {
            CmdUpdateLobby(lobbyLevelData, maxPlayers);
        }

        // On local client start
        public override void OnStartAuthority() {
            CmdSetPlayerName(Misc.Player.LocalPlayerName);
        }

        public override void OnStartClient() {
            base.OnStartClient();
            // show or hide the input field or static label depending on authority
            playerNameLabel.transform.parent.gameObject.SetActive(!hasAuthority);
            playerNameTextEntry.gameObject.SetActive(hasAuthority);
            playerNameTextEntry.interactable = !Misc.Player.IsUsingOnlineName;
            if (hasAuthority) CmdSetReadyStatus(isHost);
            UpdateDisplay();
        }

        public override void OnStopClient() {
            UpdateDisplay();
        }

        public void ToggleReady() {
            CmdSetReadyStatus(!isReady);
        }

        public void SendChatMessage(string message) {
            CmdSendMessage(message);
        }

        public void OnPlayerNameInputChanged() {
            CmdSetPlayerName(playerNameTextEntry.text);
            Preferences.Instance.SetString("playerName", playerNameTextEntry.text);
            Preferences.Instance.Save();
        }

        private void OnPlayerNameChanged(string oldName, string newName) {
            UpdateDisplay();
        }

        private void OnReadyStatusChanged(bool oldStatus, bool newStatus) {
            isReady = newStatus;
            UpdateDisplay();
        }

        private void AttachToLobbyContainer() {
            var container = GameObject.FindGameObjectWithTag("LobbyPlayerContainer");
            if (container) {
                transform.SetParent(container.transform, false);
                UpdateDisplay();
            }
        }

        private void UpdateDisplay() {
            playerNameLabel.text = playerName;
            playerNameTextEntry.text = playerName;
            readyStatus.enabled = isReady;

            // Client-side UI changes to start button
            if (LobbyUI && hasAuthority) {
                if (isHost)
                    LobbyUI.StartButton.label.text = "START GAME";
                else if (!isReady)
                    LobbyUI.StartButton.label.text = "READY";
                else
                    LobbyUI.StartButton.label.text = "UN-READY";
            }
        }

        [Command]
        private void CmdSetPlayerName(string name) {
            if (name == "") name = "UNNAMED SCRUB";

            playerName = name;
            UpdateDisplay();
        }

        [Command]
        private void CmdSetReadyStatus(bool ready) {
            isReady = ready;
        }

        [Command]
        private void CmdUpdateLobby(LevelData lobbyLevelData, short maxPlayers) {
            RpcUpdateLobby(lobbyLevelData, maxPlayers);
        }

        [Command]
        private void CmdSendMessage(string message) {
            RpcReceiveMessage(playerName + ": " + message);
        }

        [ClientRpc]
        private void RpcReceiveMessage(string message) {
            if (LobbyUI) LobbyUI.ReceiveChatMessage(message);
        }

        [ClientRpc]
        private void RpcUpdateLobby(LevelData lobbyLevelData, short maxPlayers) {
            if (!NetworkClient.isHostClient) {
                var configPanel = FindObjectOfType<LobbyConfigurationPanel>();
                if (configPanel) {
                    configPanel.maxPlayers = maxPlayers;
                    configPanel.LobbyLevelData = lobbyLevelData;
                }
            }
        }

        // On each client
        public void HostCloseLobby() {
            if (LobbyUI && LobbyUI.gameObject.activeSelf) LobbyUI.CloseLobby("The host has closed the lobby.");
        }
    }
}