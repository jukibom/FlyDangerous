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
            Debug.Log("CLIENT CONNECT");
            base.OnClientConnect(conn);
            OnClientConnected?.Invoke();
        }
        
        public override void OnClientDisconnect(NetworkConnection conn) {
            Debug.Log("CLIENT DISCONNECT");
            base.OnClientDisconnect(conn);
            OnClientDisconnected?.Invoke();
        }

        public override void OnServerConnect(NetworkConnection conn) {
            Debug.Log("SERVER CONNECT" + " (" + numPlayers + " / " + maxConnections + " players)");
            if (numPlayers >= maxConnections) {
                conn.Disconnect();
                return;
            }
        }

        public override void OnServerAddPlayer(NetworkConnection conn) {
            Debug.Log("PLAYER ADDED");
            // TODO: Handle joining in-game (this assumes we're in a lobby!)
            LobbyPlayer lobbyPlayer = Instantiate(lobbyPlayerPrefab, lobbyPlayerPrefabContainer, false);
            NetworkServer.AddPlayerForConnection(conn, lobbyPlayer.gameObject);
        }
    }
}
