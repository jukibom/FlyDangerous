using System;
using System.Collections;
using System.Collections.Generic;
using kcp2k;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core {
    
    public enum FdNetworkStatus {
        Offline,
        Lobby,
        Loading,
        InGame,
    }
    
    public class FdNetworkManager : NetworkManager {
        
        // TODO: This is game mode dependent
        [SerializeField] private int minPlayers = 2;

        [Header("Room")] 
        [SerializeField] private LobbyPlayer lobbyPlayerPrefab = null;
        
        public static event Action OnClientConnected;
        public static event Action OnClientDisconnected;
        public List<LobbyPlayer> RoomPlayers { get; } = new List<LobbyPlayer>();

        public KcpTransport networkTransport => GetComponent<KcpTransport>();

        private FdNetworkStatus _status = FdNetworkStatus.Offline;
        private FdNetworkStatus Status => _status;

        public void StartLobbyServer() {
            _status = FdNetworkStatus.Lobby;
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
            Debug.Log("[SERVER] PLAYER CONNECT" + " (" + numPlayers + " / " + maxConnections + " players)");
            if (numPlayers >= maxConnections) {
                conn.Disconnect();
                return;
            }
            
            if (conn.identity != null) {
                var player = conn.identity.GetComponent<LobbyPlayer>();
                RoomPlayers.Add(player);
            }
        }
        public override void OnServerDisconnect(NetworkConnection conn) {
            Debug.Log("[SERVER] PLAYER DISCONNECT");
                        
            if (conn.identity != null) {
                var player = conn.identity.GetComponent<LobbyPlayer>();
                RoomPlayers.Remove(player);
            }
            
            base.OnServerDisconnect(conn);
        }

        public override void OnServerAddPlayer(NetworkConnection conn) {
            Debug.Log("[SERVER] PLAYER ADDED");
            // TODO: Handle joining in-game (this assumes we're in a lobby!)
            LobbyPlayer lobbyPlayer = Instantiate(lobbyPlayerPrefab);
            lobbyPlayer.isPartyLeader = RoomPlayers.Count == 0;
            
            NetworkServer.AddPlayerForConnection(conn, lobbyPlayer.gameObject);
        }

        // Server shutdown, notify all players
        public override void OnStopClient() {
            Debug.Log("[SERVER] SHUTDOWN");
            foreach (var lobbyPlayer in RoomPlayers) {
                lobbyPlayer.CloseLobby();
            }
            RoomPlayers.Clear();
            _status = FdNetworkStatus.Offline;
        }

        public void NotifyPlayersOfReadyState() {
            foreach (var player in RoomPlayers) {
                player.HandleReadyStatusChanged(IsReadyToStart());
            }
        }

        private bool IsReadyToStart() {
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
    }
}
