using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Engine;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class JoinMenu : MonoBehaviour {
        private FdNetworkManager _fdNetworkManager;
        [SerializeField] private LobbyMenu lobbyMenu;
        [SerializeField] private Button joinButton;
        [SerializeField] private MultiPlayerMenu topMenu;
        
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
            _fdNetworkManager = FdNetworkManager.singleton as FdNetworkManager;
            gameObject.SetActive(true);
            _animator.SetBool("Open", true);
            joinButton.Select();
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        public void Cancel() {
            AudioManager.Instance.Play("ui-cancel");
            topMenu.Show();
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
            
            _fdNetworkManager.networkAddress = hostAddress;
            _fdNetworkManager.networkTransport.Port = port;
            
            _fdNetworkManager.StartClient();
            joinButton.interactable = false;
        }

        private void HandleClientConnected() {
            joinButton.interactable = true;
            Hide();
            lobbyMenu.Show();
            lobbyMenu.JoinPlayer();
        }

        private void HandleClientDisconnected() {
            joinButton.interactable = true;
            Show();
            lobbyMenu.Hide();
            // TODO: Some disconnect reason here?
        }
    }
}