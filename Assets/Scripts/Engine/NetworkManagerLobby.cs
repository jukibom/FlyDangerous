using System;
using System.Collections;
using System.Collections.Generic;
using kcp2k;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Engine {
    public class NetworkManagerLobby : NetworkManager
    {
        [Scene] [SerializeField] private string menuScene = string.Empty;

        [Header("Room")] 
        [SerializeField] private Transform lobbyPlayerPrefabContainer;
        [SerializeField] private LobbyPlayer lobbyPlayerPrefab = null;

        public static event Action OnClientConnected;
        public static event Action OnClientDisconnected;
        
        public KcpTransport networkTransport => GetComponent<KcpTransport>();

        public override void OnClientConnect(NetworkConnection conn) {
            base.OnClientConnect(conn);
            OnClientConnected?.Invoke();
        }
        
        public override void OnClientDisconnect(NetworkConnection conn) {
            base.OnClientDisconnect(conn);
            OnClientDisconnected?.Invoke();
        }

        public override void OnServerConnect(NetworkConnection conn) {
            if (numPlayers >= maxConnections) {
                conn.Disconnect();
                return;
            }

            // TODO: allow clients to connect mid-game here
            if (SceneManager.GetActiveScene().path != menuScene) {
                conn.Disconnect();
                return;
            }
        }

        public override void OnServerAddPlayer(NetworkConnection conn) {
            if (SceneManager.GetActiveScene().path == menuScene) {
                LobbyPlayer lobbyPlayer = Instantiate(lobbyPlayerPrefab, lobbyPlayerPrefabContainer, true);
                NetworkServer.AddPlayerForConnection(conn, lobbyPlayer.gameObject);
            }
        }
    }
}
