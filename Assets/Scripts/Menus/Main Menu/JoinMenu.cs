using System;
using Audio;
using Core;
using Core.Player;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class JoinMenu : MonoBehaviour {
        [SerializeField] private MainMenu mainMenu;
        [SerializeField] private LobbyMenu lobbyMenu;
        [SerializeField] private UIButton joinButton;
        [SerializeField] private MultiPlayerMenu multiPlayerMenu;
        
        [Header("Input Fields")]
        [SerializeField] private InputField playerName;
        [SerializeField] private InputField serverIPAddress;
        [SerializeField] private InputField serverPort;
        [SerializeField] private InputField serverPassword;
        private Animator _animator;

        private void Awake() {
            _animator = GetComponent<Animator>();
        }

        private void Start() {
            FdNetworkManager.OnClientConnected += HandleClientConnected;
            FdNetworkManager.OnClientDisconnected += HandleFailedConnection;
            FdNetworkManager.OnClientConnectionRejected += HandleClientRejected;
        }
        
        private void OnDestroy() {
            FdNetworkManager.OnClientConnected -= HandleClientConnected;
            FdNetworkManager.OnClientDisconnected -= HandleFailedConnection;
            FdNetworkManager.OnClientConnectionRejected -= HandleClientRejected;
        }

        private void OnEnable() {
            playerName.text = Preferences.Instance.GetString("playerName");
            serverIPAddress.text = Preferences.Instance.GetString("lastUsedServerJoinAddress");
            serverPort.text = Preferences.Instance.GetString("lastUsedServerJoinPort");
            serverPassword.text = Preferences.Instance.GetString("lastUsedServerPassword");
        }

        public void Show() {
            gameObject.SetActive(true);
            joinButton.button.interactable = true;
            joinButton.label.text = "CONNECT";
            joinButton.button.Select();
            _animator.SetBool("Open", true);
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        public void Cancel() {
            UIAudioManager.Instance.Play("ui-cancel");
            Game.Instance.SessionStatus = SessionStatus.Offline;
            FdNetworkManager.Instance.StopAll();
            multiPlayerMenu.Show();
            Hide();
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
            
            string hostAddress = serverIPAddress.text;
            ushort port = Convert.ToUInt16(Int16.Parse(serverPort.text));
            Debug.Log("Connecting to " + hostAddress + ":" + port);
            
            FdNetworkManager.Instance.networkAddress = hostAddress;
            FdNetworkManager.Instance.NetworkTransport.Port = port;

            FdNetworkManager.Instance.StartClient();
            FdNetworkManager.Instance.joinGameRequestMessage = new FdNetworkManager.JoinGameRequestMessage {
                password = serverPassword.text,
                version = Application.version
            };
            joinButton.button.interactable = false;
            joinButton.label.text = "CONNECTING ...";
        }

        private void HandleClientConnected(FdNetworkManager.JoinGameSuccessMessage successMessage) {
            Hide();
            
            // if the server has created a lobby player for us, show the lobby
            if (successMessage.showLobby) {
                Game.Instance.SessionStatus = SessionStatus.LobbyMenu;
                lobbyMenu.Show();
                lobbyMenu.JoinPlayer();
                
                var localPlayer = LobbyPlayer.FindLocal;
                if (localPlayer) {
                    localPlayer.UpdateLobby(successMessage.levelData, successMessage.maxPlayers);
                }
            }
        }

        private void HandleFailedConnection() {
            joinButton.button.interactable = true;
            joinButton.label.text = "CONNECT";
            joinButton.button.Select();
        }

        private void HandleClientRejected(string reason) {
            Hide();
            mainMenu.ShowDisconnectedDialog(reason);
        }
    }
}