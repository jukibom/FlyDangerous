using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Menus.Main_Menu;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Player {
    public class LobbyPlayer : NetworkBehaviour {
        
        [CanBeNull]
        public static LobbyPlayer FindLocal => 
            Array.Find(FindObjectsOfType<LobbyPlayer>(), lobbyPlayer => lobbyPlayer.isLocalPlayer);
        
        [SyncVar] public bool isHost;

        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        public string playerName = "Connecting ...";
        
        [SyncVar(hook = nameof(OnPlayerReadyStatusChanged))]
        public bool isReady;

        [SerializeField] private Text playerNameTextEntry;
        [SerializeField] private RawImage readyStatus;

        private LobbyMenu _lobby;
        private LobbyMenu LobbyUI {
            get
            {
                if (_lobby == null) {
                    _lobby = FindObjectOfType<LobbyMenu>();
                }

                return _lobby;
            }
        }

        void Start() {
            DontDestroyOnLoad(this);
        }

        private void FixedUpdate() {
            if (!transform.parent) {
                AttachToLobbyContainer();
            }
        }

        public void UpdateLobby(LevelData lobbyLevelData) {
            CmdUpdateLobby(lobbyLevelData);
        }

        // On local client start
        public override void OnStartAuthority() {
            CmdSetPlayerName(Preferences.Instance.GetString("playerName"));
        }

        public override void OnStartClient() {
            UpdateDisplay();
        }

        public override void OnStopClient() {
            UpdateDisplay();
        }

        public void ToggleReady() {
            isReady = !isReady;
        }

        public void HandleReadyStatusChanged(bool ready) {
            isReady = ready;
        }

        private void OnPlayerNameChanged(string oldName, string newName) => UpdateDisplay();
        private void OnPlayerReadyStatusChanged(bool oldStatus, bool newStatus) => UpdateDisplay();

        private void AttachToLobbyContainer() {
            var container = GameObject.FindGameObjectWithTag("LobbyPlayerContainer");
            if (container) {
                transform.SetParent(container.transform, false);
                UpdateDisplay();
            }
        }
        
        private void UpdateDisplay() {
            playerNameTextEntry.text = playerName;
            readyStatus.enabled = isReady;
            if (LobbyUI) {
                if (isHost && !isReady) {
                    LobbyUI.StartButton.label.text = "START GAME";
                }
                else if (!isReady) {
                    LobbyUI.StartButton.label.text = "READY";
                }
                else {
                    LobbyUI.StartButton.label.text = "UN-READY";
                }
            }
        }

        [Command]
        private void CmdSetPlayerName(string name) {
            if (name == "") {
                name = "UNNAMED SCRUB";
            }

            playerName = name;
            UpdateDisplay();
        }

        [Command]
        private void CmdUpdateLobby(LevelData lobbyLevelData) {
            RpcUpdateLobby(lobbyLevelData);
        }

        [ClientRpc]
        private void RpcUpdateLobby(LevelData lobbyLevelData) {
            if (!NetworkClient.isHostClient) {
                var configPanel = FindObjectOfType<LobbyConfigurationPanel>();
                if (configPanel) {
                    configPanel.LobbyLevelData = lobbyLevelData;
                }
            }
        }

        // On each client
        public void CloseLobby() {
            if (LobbyUI && LobbyUI.gameObject.activeSelf) {
                LobbyUI.CloseLobby();
            }
        }
    }
}