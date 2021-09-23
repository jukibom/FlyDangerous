using System;
using System.Collections;
using System.Collections.Generic;
using Core.MapData;
using Core.Player;
using kcp2k;
using Mirror;
using Misc;
using UnityEngine;

namespace Core {
    public class FdNetworkManager : NetworkManager {
        
        public const short maxPlayerLimit = 128;
        
        public static FdNetworkManager Instance => singleton as FdNetworkManager;

        [Header("Room")] 
        [SerializeField] private LobbyPlayer lobbyPlayerPrefab;

        [Header("Loading")] 
        [SerializeField] private LoadingPlayer loadingPlayerPrefab;

        [Header("In-Game")] 
        [SerializeField] private ShipPlayer shipPlayerPrefab;

        public struct JoinGameRequestMessage : NetworkMessage {
            public string password;
            public string version;
        }
        
        public struct JoinGameSuccessMessage : NetworkMessage {
            public bool showLobby;
            public LevelData levelData;
            public short maxPlayers;
        }
        
        public struct JoinGameRejectionMessage : NetworkMessage {
            public string reason;
        }
        
        private struct StartGameMessage : NetworkMessage {
            public SessionType sessionType;
            public LevelData levelData;
        }

        private struct ReturnToLobbyMessage : NetworkMessage {};

        private struct SetShipPositionMessage : NetworkMessage {
            public Vector3 position;
            public Quaternion rotation;
        }
        
        public static event Action<JoinGameSuccessMessage> OnClientConnected;
        public static event Action OnClientDisconnected;
        public static event Action<string> OnClientConnectionRejected;
        public List<LobbyPlayer> LobbyPlayers { get; } = new List<LobbyPlayer>();
        public List<LoadingPlayer> LoadingPlayers { get; } = new List<LoadingPlayer>();
        public List<ShipPlayer> ShipPlayers { get; } = new List<ShipPlayer>();
        public KcpTransport NetworkTransport => GetComponent<KcpTransport>();

        // Mirror's transport layer IMMEDIATELY severs the connection if the internal maxConnections limit is exceeded.
        // To work around this and have some sane messaging back to clients, we're using a server limit of 129 and
        // a "true" limit of 128. The additional slot is there just to allow a client to connect, receive a message
        // explaining why they're being kicked ... and then kicked. Aren't distributed systems fun?
        public short maxPlayers = maxPlayerLimit;

        // set to non-empty string to force password validation on connecting clients
        public static string serverPassword;

        // TODO: This is gross (client attach object before connection attempt) but I'm almost out of fucks to give
        public JoinGameRequestMessage joinGameRequestMessage;
        
        #region Start / Quit Game
        public void StartGameLoadSequence(SessionType sessionType, LevelData levelData) {
            if (NetworkServer.active) {
                // Transition any lobby players to loading state
                if (Game.Instance.SessionStatus == SessionStatus.LobbyMenu) {
                    
                    // iterate over a COPY of the lobby players (the List is mutated by transitioning!)
                    foreach (var lobbyPlayer in LobbyPlayers.ToArray()) {
                        TransitionToLoadingPlayer(lobbyPlayer);
                    }
                }

                // notify all clients about the new scene
                NetworkServer.SendToAll(new StartGameMessage {
                    sessionType = sessionType, 
                    levelData = levelData
                });
            }
            else {
                throw new Exception("Cannot start a game without an active server!");
            }
        }

        public void StartReturnToLobbySequence() {
            if (NetworkServer.active) {
                // Transition any lobby players to loading state
                if (Game.Instance.SessionStatus == SessionStatus.InGame) {
                    // iterate over a COPY of the lobby players (the List is mutated by transitioning!)
                    foreach (var shipPlayer in ShipPlayers.ToArray()) {
                        TransitionToLobbyPlayer(shipPlayer);
                    }
                }

                // notify all clients about the new scene
                NetworkServer.SendToAll(new ReturnToLobbyMessage());
            }
            else {
                throw new Exception("Cannot return to lobby without an active server!");
            }
        }

        [Server]
        public void LoadPlayerShip(LoadingPlayer loadingPlayer) {
            try {
                var ship = TransitionToShipPlayer(loadingPlayer);
                var levelData = Game.Instance.LoadedLevelData;
                
                // handle start position for each client
                var position = new Vector3(
                    levelData.startPosition.x,
                    levelData.startPosition.y,
                    levelData.startPosition.z
                );
                var rotation = Quaternion.Euler(
                    levelData.startRotation.x,
                    levelData.startRotation.y,
                    levelData.startRotation.z
                );

                // TODO: radius should possibly be determined by the ship model itself!
                position = PositionalHelpers.FindClosestEmptyPosition(position, 10);

                // update locally immediately for subsequent collision checks
                ship.AbsoluteWorldPosition = position;
                ship.transform.rotation = rotation;

                // ensure each client receives their assigned position
                ship.connectionToClient.Send(new SetShipPositionMessage {
                    position = position,
                    rotation = rotation
                });

                // Update physics engine so subsequent collision checks are up-to-date
                Physics.SyncTransforms();
                

                // all ships created and placed, notify ready (allows them to start syncing their own positions)
                foreach (var shipPlayer in ShipPlayers) {
                    shipPlayer.ServerReady();
                }
            }
            catch {
                Game.Instance.QuitToMenu("The server failed to initialise properly");
            }
        }
        #endregion
        
        #region State Management

        public void StopAll() {
            if (mode != NetworkManagerMode.Offline) {
                StopHost();
                StopClient();
            }
        }
        
        #endregion

        #region Client Handlers

        // player joins
        public override void OnClientConnect(NetworkConnection conn) {
            Debug.Log("[CLIENT] LOCAL CLIENT HAS CONNECTED");
            base.OnClientConnect(conn);
            NetworkClient.RegisterHandler<JoinGameSuccessMessage>(OnJoinGameClientMsg);
            NetworkClient.RegisterHandler<JoinGameRejectionMessage>(OnRejectConnectionClientMsg);
            NetworkClient.RegisterHandler<StartGameMessage>(OnStartLoadGameClientMsg);
            NetworkClient.RegisterHandler<ReturnToLobbyMessage>(OnShowLobbyClientMsg);
            NetworkClient.RegisterHandler<SetShipPositionMessage>(OnSetShipPositionClientMsg);
            
            // Initiate handshake
            conn.Send(joinGameRequestMessage);
        }
        
        // player leaves
        public override void OnClientDisconnect(NetworkConnection conn) {
            Debug.Log("[CLIENT] LOCAL CLIENT HAS DISCONNECTED");
            base.OnClientDisconnect(conn);
            OnClientDisconnected?.Invoke();
        }
        
        // Server shutdown, notify all players
        public override void OnStopClient() {
            Debug.Log("[CLIENT] SERVER SHUTDOWN");
            switch (Game.Instance.SessionStatus) {
                
                case SessionStatus.LobbyMenu:
                    var localPlayer = LobbyPlayer.FindLocal;
                    if (localPlayer) {
                        localPlayer.HostCloseLobby();
                    }
                    break;
                
                case SessionStatus.Loading:
                    Game.Instance.QuitToMenu("LOST CONNECTION TO THE SERVER WHILE LOADING.");
                    break;
                
                case SessionStatus.InGame:
                    Game.Instance.QuitToMenu("THE SERVER CLOSED THE CONNECTION");
                    break;
                    
            }

            Game.Instance.SessionStatus = SessionStatus.Offline;
        }

        #endregion
        
        #region Server Handlers
        
        public override void OnStartServer() {
            base.OnStartServer();
            NetworkServer.RegisterHandler<JoinGameRequestMessage>(OnJoinGameServerMsg);
        }

        // player joins
        public override void OnServerConnect(NetworkConnection conn) {
            Debug.Log("[SERVER] PLAYER CONNECT" + " (" + (numPlayers + 1) + " / " + maxPlayers + " players)");
            
            if (numPlayers >= maxPlayers) {
                RejectPlayerConnection(conn, "Server is at max player limit.");
                return;
            }

            if (Game.Instance.SessionStatus != SessionStatus.LobbyMenu) {
                // joining mid-game checks
                if (!Game.Instance.IsGameHotJoinable) {
                    RejectPlayerConnection(conn , "Game is currently running and does not permit joining during the match.");
                }
            }
        }
        
        // player leaves
        public override void OnServerDisconnect(NetworkConnection conn) {
            Debug.Log("[SERVER] PLAYER DISCONNECT");
                        
            // TODO: notify other players than someone has left
            if (conn.identity != null) {
                switch (Game.Instance.SessionStatus) {
                    case SessionStatus.SinglePlayerMenu:
                        var loadingPlayer = conn.identity.GetComponent<LoadingPlayer>();
                        RemovePlayer(loadingPlayer);
                        break;
                    
                    case SessionStatus.LobbyMenu:
                        var lobbyPlayer = conn.identity.GetComponent<LobbyPlayer>();
                        RemovePlayer(lobbyPlayer);
                        break;  
                    
                    case SessionStatus.InGame:
                        var shipPlayer = conn.identity.GetComponent<ShipPlayer>();
                        RemovePlayer(shipPlayer);
                        break;
                }
            }
            
            base.OnServerDisconnect(conn);
        }

        public override void OnStopServer() {
            LobbyPlayers.Clear();
            LoadingPlayers.Clear();
            ShipPlayers.Clear();
        }

        #endregion

        #region Player Transition + List Management

        private LobbyPlayer TransitionToLobbyPlayer<T>(T previousPlayer) where T: NetworkBehaviour {
            var lobbyPlayer = Instantiate(lobbyPlayerPrefab);
            lobbyPlayer.isHost = GetHostStatus(previousPlayer);
            return ReplacePlayer(lobbyPlayer, previousPlayer);
        }
        
        private LoadingPlayer TransitionToLoadingPlayer<T>(T previousPlayer) where T: NetworkBehaviour {
            var loadingPlayer = Instantiate(loadingPlayerPrefab);
            loadingPlayer.isHost = GetHostStatus(previousPlayer);
            return ReplacePlayer(loadingPlayer, previousPlayer);
        }
        
        private ShipPlayer TransitionToShipPlayer<T>(T previousPlayer) where T: NetworkBehaviour {
            var shipPlayer = Instantiate(shipPlayerPrefab);
            shipPlayer.isHost = GetHostStatus(previousPlayer);
            return ReplacePlayer(shipPlayer, previousPlayer);
        }
        
        private T ReplacePlayer<T, TU>(T newPlayer, TU previousPlayer) where T : NetworkBehaviour where TU : NetworkBehaviour {
            Debug.Log("REPLACE PLAYER " + previousPlayer + " " + previousPlayer.connectionToClient + " " + newPlayer);
            var conn = previousPlayer.connectionToClient;
            if (previousPlayer.connectionToClient.identity != null) {
                NetworkServer.Destroy(previousPlayer.connectionToClient.identity.gameObject);
                NetworkServer.ReplacePlayerForConnection(conn, newPlayer.gameObject, true);
                AddPlayer(newPlayer);
            }
            else {
                Debug.LogWarning("Null connection found when replacing player - skipping.");
            }
            RemovePlayer(previousPlayer);
            return newPlayer;
        }

        private T AddPlayerFromPrefab<T>(NetworkConnection conn, T prefab) where T : NetworkBehaviour {
            T playerConnectionPrefab = Instantiate(prefab);
            NetworkServer.AddPlayerForConnection(conn, playerConnectionPrefab.gameObject);
            if (conn.identity != null) {
                var player = conn.identity.GetComponent<T>();
                AddPlayer(player);
                return player;
            }
            else {
                throw new Exception("Failed to add player with null connection!");
            }
        }
        
        private void AddPlayer<T>(T player) where T : NetworkBehaviour {
            switch (player) {
                case LobbyPlayer lobbyPlayer: LobbyPlayers.Add(lobbyPlayer);
                    break;
                case LoadingPlayer loadingPlayer: LoadingPlayers.Add(loadingPlayer);
                    break;
                case ShipPlayer shipPlayer: ShipPlayers.Add(shipPlayer);
                    break;
                default:
                    throw new Exception("Unsupported player object type!");
            }
        }

        private void RemovePlayer<T>(T player) where T : NetworkBehaviour {
            if (player != null) {
                switch (player) {
                    case LobbyPlayer lobbyPlayer:
                        LobbyPlayers.Remove(lobbyPlayer);
                        break;
                    case LoadingPlayer loadingPlayer:
                        LoadingPlayers.Remove(loadingPlayer);
                        break;
                    case ShipPlayer shipPlayer:
                        ShipPlayers.Remove(shipPlayer);
                        break;
                    default:
                        throw new Exception("Unsupported player object type!");
                }
            }
        }

        private bool GetHostStatus<T>(T player) where T : NetworkBehaviour {
            if (player != null) {
                switch (player) {
                    case LobbyPlayer lobbyPlayer:
                        return lobbyPlayer.isHost;
                    case LoadingPlayer loadingPlayer:
                        return loadingPlayer.isHost;
                    case ShipPlayer shipPlayer:
                        return shipPlayer.isHost;
                    default:
                        throw new Exception("Unsupported player object type!");
                }
            }

            return false;
        }
        
        #endregion

        #region Helpers
        
        /**
         * This handler is a bit of a beast so let's break it down.
         * Mirror requires an active connection tied to an entity to send messages to them. In order to "reject" a
         * connection we must first, therefore, accept it. This means that our Mirror maxConnections is always at least
         * 1 more than the actual maximum to accomodate this. This function creates a loading player prefab, adds the
         * client as an active connection and starts a Coroutine to wait for ready status before sending a message that
         * the client subscribes to and then forcibly disconnects the client so that a reason can be displayed.
         */
        private void RejectPlayerConnection(NetworkConnection conn, string reason) {
            LoadingPlayer loadingPlayer = Instantiate(loadingPlayerPrefab);
            NetworkServer.AddPlayerForConnection(conn, loadingPlayer.gameObject);
            
            IEnumerator Reject() {
                while (!conn.isReady) {
                    yield return new WaitForEndOfFrame();
                }
                conn.identity.connectionToClient.Send(new JoinGameRejectionMessage { reason = reason });
                yield return new WaitForEndOfFrame();

                conn.Disconnect();
            }

            StartCoroutine(Reject());
        }

        #endregion
        
        
        #region Server Message Handlers

        private void OnJoinGameServerMsg(NetworkConnection conn, JoinGameRequestMessage message) { 
            IEnumerator AddNewPlayerConnection() {
                while (!conn.isReady) {
                    yield return new WaitForEndOfFrame();
                }

                // prevent server checks against connection requests on a HostClient
                var isLocalConnection = conn.connectionId == NetworkClient.connection.connectionId;

                // authentication
                if (!isLocalConnection && !string.IsNullOrEmpty(serverPassword)) {
                    if (serverPassword != message.password) {
                        RejectPlayerConnection(conn, "Could not authenticate: incorrect password");
                        yield break;
                    }
                }
                
                // version check
                if (!isLocalConnection) {
                    if (Application.version != message.version) {
                        RejectPlayerConnection(conn, "Fly Dangerous version mismatch! Server is running version " + Application.version);
                    }
                }

                var sessionStatus = Game.Instance.SessionStatus;
                var levelData = Game.Instance.LoadedLevelData;;
                
                switch (sessionStatus) {
                    case SessionStatus.Development:
                        var shipPlayer = AddPlayerFromPrefab(conn, shipPlayerPrefab);
                        shipPlayer.ServerReady();
                        break;
                    
                    case SessionStatus.SinglePlayerMenu:
                    case SessionStatus.InGame:
                    case SessionStatus.Loading:
                        AddPlayerFromPrefab(conn, loadingPlayerPrefab);

                        // if we're joining mid-game (or single player), attempt to start the single client
                        if (sessionStatus != SessionStatus.SinglePlayerMenu) {
                            conn.identity.connectionToClient.Send( new StartGameMessage {
                                sessionType = SessionType.Multiplayer, 
                                levelData = levelData
                            });
                        }
                        break;
                
                    case SessionStatus.LobbyMenu:
                        // Fetch host lobby config to send to client instead of game state loaded level data
                        var hostLobbyConfigurationPanel = FindObjectOfType<LobbyConfigurationPanel>();
                        if (hostLobbyConfigurationPanel) {
                            levelData = hostLobbyConfigurationPanel.LobbyLevelData;
                        }

                        LobbyPlayer lobbyPlayer = AddPlayerFromPrefab(conn, lobbyPlayerPrefab);
                        lobbyPlayer.isHost = LobbyPlayers.Count == 1;
                        
                        break;
                    
                    case SessionStatus.Offline:
                        // I don't know how the hell we get here but something gone bonkers if we do
                        Debug.LogError("Somehow tried to get a connection without being online!");
                        conn.Disconnect();
                        break;
                }

                conn.identity.connectionToClient.Send(new JoinGameSuccessMessage {
                        showLobby = sessionStatus == SessionStatus.LobbyMenu, 
                        levelData = levelData,
                        maxPlayers = maxPlayers
                    }
                );
            }

            StartCoroutine(AddNewPlayerConnection());
        }
        
        #endregion
        
        
        #region Client Message Handlers
        
        private void OnJoinGameClientMsg(JoinGameSuccessMessage successMessage) {
            OnClientConnected?.Invoke(successMessage);
        }
        
        private void OnRejectConnectionClientMsg(JoinGameRejectionMessage message) {
            Debug.Log(message.reason);
            OnClientConnectionRejected?.Invoke(message.reason);
        }

        private void OnStartLoadGameClientMsg(StartGameMessage message) {
            Game.Instance.StartGame(message.sessionType, message.levelData);
        }

        private void OnShowLobbyClientMsg(ReturnToLobbyMessage message) {
            Game.Instance.QuitToLobby();
        }
        
        private void OnSetShipPositionClientMsg(SetShipPositionMessage message) {
            var ship = ShipPlayer.FindLocal;
            if (ship) {
                ship.AbsoluteWorldPosition = message.position;
                ship.transform.rotation = message.rotation;
            }
        }
        
        #endregion
    }
}
