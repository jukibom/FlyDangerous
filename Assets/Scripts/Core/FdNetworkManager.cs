using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Player;
using kcp2k;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core {
    
    public enum FdNetworkStatus {
        SinglePlayerMenu,
        LobbyMenu,
        Loading,
        InGame,
    }
    
    public class FdNetworkManager : NetworkManager {
        
        public static FdNetworkManager Instance => singleton as FdNetworkManager;
        
        // TODO: This is game mode dependent
        [SerializeField] private int minPlayers = 2;

        [Header("Room")] 
        [SerializeField] private LobbyPlayer lobbyPlayerPrefab;

        [Header("Loading")] 
        [SerializeField] private LoadingPlayer loadingPlayerPrefab;

        [Header("In-Game")] 
        [SerializeField] private ShipPlayer shipPlayerPrefab;
        
        public static event Action OnClientConnected;
        public static event Action OnClientDisconnected;
        public List<LobbyPlayer> RoomPlayers { get; } = new List<LobbyPlayer>();
        public List<LoadingPlayer> LoadingPlayers { get; } = new List<LoadingPlayer>();
        public List<ShipPlayer> ShipPlayers { get; } = new List<ShipPlayer>();
        public KcpTransport NetworkTransport => GetComponent<KcpTransport>();

        private FdNetworkStatus _status = FdNetworkStatus.SinglePlayerMenu;
        private FdNetworkStatus Status => _status;

        public void StartLobbyServer() {
            _status = FdNetworkStatus.LobbyMenu;
            StartHost();
            // TODO: This should come from the lobby panel UI element
            maxConnections = 16;
        }

        public void StartLobbyJoin() {
            _status = FdNetworkStatus.LobbyMenu;
            StartClient();
        }

        public void StartOfflineServer() {
            _status = FdNetworkStatus.SinglePlayerMenu;
            maxConnections = 1;
            StartHost();
        }
        }
        public void CloseConnection() {
            if (mode != NetworkManagerMode.Offline) {
                StopHost();
                StopClient();
            }
            _status = FdNetworkStatus.SinglePlayerMenu;
        }

        // --- LOCAL CLIENT SIDE PLAYER CONNECTIONS --- //
        public override void OnClientConnect(NetworkConnection conn) {
            Debug.Log("[CLIENT] PLAYER CONNECT");
            base.OnClientConnect(conn);
            OnClientConnected?.Invoke();
        }
        public override void OnClientDisconnect(NetworkConnection conn) {
            Debug.Log("[CLIENT] PLAYER DISCONNECT");
            base.OnClientDisconnect(conn);
            OnClientDisconnected?.Invoke();
        }

        // --- SERVER SIDE PLAYER CONNECTIONS --- //
        public override void OnServerConnect(NetworkConnection conn) {
            Debug.Log("[SERVER] PLAYER CONNECT" + " (" + numPlayers + 1 + " / " + maxConnections + " players)");
            if (numPlayers >= maxConnections) {
                conn.Disconnect();
                // TODO: Send a message why
                return;
            }
        }
        public override void OnServerDisconnect(NetworkConnection conn) {
            Debug.Log("[SERVER] PLAYER DISCONNECT");
                        
            if (conn.identity != null) {
                switch (Status) {
                    case FdNetworkStatus.SinglePlayerMenu:
                        var loadingPlayer = conn.identity.GetComponent<LoadingPlayer>();
                        LoadingPlayers.Remove(loadingPlayer);
                        break;
                    
                    case FdNetworkStatus.LobbyMenu:
                        var lobbyPlayer = conn.identity.GetComponent<LobbyPlayer>();
                        RoomPlayers.Remove(lobbyPlayer);
                        break;
                }
                
            }
            
            base.OnServerDisconnect(conn);
        }

        // Server shutdown, notify all players
        public override void OnStopClient() {
            Debug.Log("[SERVER] SHUTDOWN");
            switch (Status) {
                
                case FdNetworkStatus.LobbyMenu:
                    foreach (var lobbyPlayer in RoomPlayers) {
                        lobbyPlayer.CloseLobby();
                    }
                    RoomPlayers.Clear();
                    break;
                
                case FdNetworkStatus.Loading:
                    // foreach (var loadingPlayer in LoadingPlayers) {
                    //     
                    // }
                    LoadingPlayers.Clear();
                    break;
                
                case FdNetworkStatus.InGame:
                    break;
                    
            }
            _status = FdNetworkStatus.SinglePlayerMenu;
        }
        
        public IEnumerator WaitForAllPlayersLoaded() {
            yield return LoadingPlayers.All(loadingPlayer => loadingPlayer.IsLoaded) 
                ? null 
                : new WaitForFixedUpdate();
        }

        public void NotifyPlayersOfReadyState() {
            foreach (var player in RoomPlayers) {
                player.HandleReadyStatusChanged(IsReadyToLoad());
            }
        }

        private bool IsReadyToLoad() {
            if (numPlayers < minPlayers) {
                return false; 
            }

            foreach (var player in RoomPlayers) {
                if (!player.isReady) {
                    return false; 
                }
            }

            return true;
        }

        // hijack the auto add player functionality and add our own depending on context
        public override void OnServerAddPlayer(NetworkConnection conn) {
            Debug.Log("[SERVER] PLAYER ADDED");
            switch (Status) {
                
                case FdNetworkStatus.SinglePlayerMenu:
                    LoadingPlayer loadingPlayer = Instantiate(loadingPlayerPrefab);
                    NetworkServer.AddPlayerForConnection(conn, loadingPlayer.gameObject);
                    if (conn.identity != null) {
                        var player = conn.identity.GetComponent<LoadingPlayer>();
                        LoadingPlayers.Add(player);
                    }
                    break;
                
                case FdNetworkStatus.LobbyMenu: 
                    LobbyPlayer lobbyPlayer = Instantiate(lobbyPlayerPrefab);
                    lobbyPlayer.isPartyLeader = RoomPlayers.Count == 0;
            
                    NetworkServer.AddPlayerForConnection(conn, lobbyPlayer.gameObject);
                    
                    if (conn.identity != null) {
                        var player = conn.identity.GetComponent<LobbyPlayer>();
                        RoomPlayers.Add(player);
                    }
                    break;
            }
        }
    }
}
