using System;
using Audio;
using Core;
using Core.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class JoinMenu : MonoBehaviour {
        [SerializeField] private LobbyMenu lobbyMenu;
        [SerializeField] private Button joinButton;
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

        private void OnEnable() {
            FdNetworkManager.OnClientConnected += HandleClientConnected;
            FdNetworkManager.OnClientDisconnected += HandleClientDisconnected;
            playerName.text = Preferences.Instance.GetString("playerName");
            serverIPAddress.text = Preferences.Instance.GetString("lastUsedServerJoinAddress");
            serverPort.text = Preferences.Instance.GetString("lastUsedServerJoinPort");
        }

        private void OnDisable() {
            FdNetworkManager.OnClientConnected -= HandleClientConnected;
            FdNetworkManager.OnClientDisconnected -= HandleClientDisconnected;
        }

        public void Show() {
            gameObject.SetActive(true);
            joinButton.interactable = true;
            joinButton.Select();
            _animator.SetBool("Open", true);
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        public void Cancel() {
            AudioManager.Instance.Play("ui-cancel");
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
            Preferences.Instance.Save();
        }
        
        public void Join() {
            string hostAddress = serverIPAddress.text;
            ushort port = Convert.ToUInt16(Int16.Parse(serverPort.text));
            Debug.Log("Connecting to " + hostAddress + ":" + port);
            
            FdNetworkManager.Instance.networkAddress = hostAddress;
            FdNetworkManager.Instance.NetworkTransport.Port = port;
            
            FdNetworkManager.Instance.StartClient();
            joinButton.interactable = false;
        }

        private void HandleClientConnected(FdNetworkManager.JoinGameMessage message) {
            joinButton.interactable = true;
            Hide();
            
            // if the server has created a lobby player for us, show the lobby
            if (message.showLobby) {
                Game.Instance.SessionStatus = SessionStatus.LobbyMenu;
                lobbyMenu.Show();
                lobbyMenu.JoinPlayer();
                
                LobbyPlayer.FindLocal.UpdateLobby(message.levelData);
            }
        }

        private void HandleClientDisconnected() {
            joinButton.interactable = true;
            Show();
            lobbyMenu.Hide();
            // TODO: Some disconnect reason here?
        }
    }
}