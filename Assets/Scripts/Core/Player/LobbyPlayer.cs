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
        
        public bool isPartyLeader;

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
            Debug.Log("*** PLAYER PREFAB CREATED ***");
            // Attach self to the player list
            var container = GameObject.FindGameObjectWithTag("LobbyPlayerContainer");
            if (container) {
                transform.SetParent(container.transform, false);
            }
        }

        // On local client start
        public override void OnStartAuthority() {
            CmdSetPlayerName(Preferences.Instance.GetString("playerName"));
        }

        public override void OnStartClient() {
            FdNetworkManager.Instance.LobbyPlayers.Add(this);
            UpdateDisplay();
        }

        public override void OnStopClient() {
            FdNetworkManager.Instance.LobbyPlayers.Remove(this);
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
        
        private void UpdateDisplay() {
            playerNameTextEntry.text = playerName;
            readyStatus.enabled = isReady;
            if (isPartyLeader && !isReady) {
                LobbyUI.StartButton.label.text = "START GAME";
            } else if (!isReady) {
                LobbyUI.StartButton.label.text = "READY";
            }
            else {
                LobbyUI.StartButton.label.text = "UN-READY";
            }
        }

        [Command]
        public void CmdSetPlayerName(string name) {
            if (name == "") {
                name = "UNNAMED SCRUB";
            }

            playerName = name;
            UpdateDisplay();
        }
        
        // On each client
        public void CloseLobby() {
            LobbyUI.CloseLobby();
        }
    }
}