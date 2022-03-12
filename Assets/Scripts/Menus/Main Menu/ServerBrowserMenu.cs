using System;
using Core;
using Menus.Main_Menu.Components;
using UnityEngine;

namespace Menus.Main_Menu {
    public class ServerBrowserMenu : MenuBase {
        [SerializeField] private LobbyMenu lobbyMenu;
        [SerializeField] private ConnectingDialog connectingDialog;
        [SerializeField] private MultiPlayerMenu lanMenu;

        [SerializeField] private GameObject refreshingIndicator;
        [SerializeField] private Transform serverEntryContainer;
        [SerializeField] private ServerBrowserEntry serverBrowserEntryPrefab;

        public LobbyMenu LobbyMenu => lobbyMenu;
        public ConnectingDialog ConnectingDialog => connectingDialog;

        protected override void OnOpen() {
            RefreshList();
        }

        public void OnRefresh() {
            PlayApplySound();
            RefreshList();
        }

        public void OpenLanPanel() {
            Progress(lanMenu);
        }

        public void OpenHostPanel() {
            Game.Instance.SessionStatus = SessionStatus.LobbyMenu;
            Progress(lobbyMenu);
            lobbyMenu.StartHost();
        }

        public void ClosePanel() {
            Cancel();
        }

        private async void RefreshList() {
            PlayApplySound();
            if (FdNetworkManager.Instance.HasMultiplayerServices)
                try {
                    var existingEntries = serverEntryContainer.gameObject.GetComponentsInChildren<ServerBrowserEntry>();
                    foreach (var serverBrowserEntry in existingEntries) Destroy(serverBrowserEntry.gameObject);

                    refreshingIndicator.SetActive(true);
                    var servers = await FdNetworkManager.Instance.OnlineService!.Multiplayer!.GetLobbyList();
                    refreshingIndicator.SetActive(false);

                    foreach (var serverId in servers) {
                        var serverEntry = Instantiate(serverBrowserEntryPrefab, serverEntryContainer);
                        serverEntry.LobbyId = serverId;
                    }

                    foreach (var serverEntry in serverEntryContainer.GetComponentsInChildren<ServerBrowserEntry>())
                        if (serverEntry != null)
                            await serverEntry.Refresh();
                }
                // Discard cancellation exceptions - retrying before completion will cancel pending operations.
                // This is intended.
                catch (OperationCanceledException) {
                }
        }
    }
}