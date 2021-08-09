using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Player;
using JetBrains.Annotations;
using kcp2k;
using Mirror;
using Misc;
using UnityEngine;

namespace Core {
    
    public enum FdNetworkStatus {
        Offline,
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

        private struct StartGameMessage : NetworkMessage {
            public SessionType sessionType;
            public LevelData levelData;
        }

        private struct ReturnToLobbyMessage : NetworkMessage {};

            private struct SetShipPositionMessage : NetworkMessage {
            public Vector3 position;
            public Quaternion rotation;
        }
        
        public static event Action OnClientConnected;
        public static event Action OnClientDisconnected;
        public List<LobbyPlayer> LobbyPlayers { get; } = new List<LobbyPlayer>();
        public List<LoadingPlayer> LoadingPlayers { get; } = new List<LoadingPlayer>();
        public List<ShipPlayer> ShipPlayers { get; } = new List<ShipPlayer>();
        public KcpTransport NetworkTransport => GetComponent<KcpTransport>();

        private FdNetworkStatus _status = FdNetworkStatus.Offline;

        #region Start / Quit Game
        public void StartGameLoadSequence(SessionType sessionType, LevelData levelData, bool dynamicPlacement = false) {
            if (NetworkServer.active) {
                // Transition any lobby players to loading state
                if (_status == FdNetworkStatus.LobbyMenu) {
                    
                    // iterate over a COPY of the lobby players (the List is mutated by transitioning!)
                    foreach (var lobbyPlayer in LobbyPlayers.ToArray()) {
                        TransitionToLoadingPlayer(lobbyPlayer);
                    }
                }
                _status = FdNetworkStatus.Loading;

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
                if (_status == FdNetworkStatus.InGame) {
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
        public void StartMainGame() {
            _status = FdNetworkStatus.InGame;
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
        
        public void StartLobbyServer() {
            _status = FdNetworkStatus.LobbyMenu;
            // TODO: This should come from the lobby panel UI element
            maxConnections = 16;
            StartHost();
        }

        public void StartOfflineServer() {
            _status = FdNetworkStatus.SinglePlayerMenu;
            maxConnections = 1;
            StartHost();
        }

        public void StopAll() {
            if (mode != NetworkManagerMode.Offline) {
                StopHost();
                StopClient();
            }
            _status = FdNetworkStatus.Offline;
        }
        
        #endregion

        #region Client Handlers

        // player joins
        public override void OnClientConnect(NetworkConnection conn) {
            Debug.Log("[CLIENT] LOCAL CLIENT HAS CONNECTED");
            base.OnClientConnect(conn);
            OnClientConnected?.Invoke();
            NetworkClient.RegisterHandler<StartGameMessage>(StartLoadGame);
            NetworkClient.RegisterHandler<ReturnToLobbyMessage>(ShowLobby);
            NetworkClient.RegisterHandler<SetShipPositionMessage>(SetShipPosition);
        }
        
        // player leaves
        public override void OnClientDisconnect(NetworkConnection conn) {
            Debug.Log("[CLIENT] LOCAL CLIENT HAS DISCONNECTED");
            base.OnClientDisconnect(conn);
            OnClientDisconnected?.Invoke();
        }

        #endregion
        
        #region Server Handlers

        // player joins
        public override void OnServerConnect(NetworkConnection conn) {
            Debug.Log("[SERVER] PLAYER CONNECT" + " (" + (numPlayers + 1) + " / " + maxConnections + " players)");
            if (numPlayers >= maxConnections) {
                conn.Disconnect();
                // TODO: Send a message why
                return;
            }

            IEnumerator AddNewPlayerConnection() {
                while (!conn.isReady) {
                    yield return new WaitForEndOfFrame();
                }
                
                switch (_status) {
                    case FdNetworkStatus.SinglePlayerMenu:
                    case FdNetworkStatus.InGame:
                    case FdNetworkStatus.Loading:
                        LoadingPlayer loadingPlayer = Instantiate(loadingPlayerPrefab);
                        NetworkServer.AddPlayerForConnection(conn, loadingPlayer.gameObject);
                        if (conn.identity != null) {
                            var player = conn.identity.GetComponent<LoadingPlayer>();
                            AddPlayer(player);
                        }

                        // if we're joining mid-game (not single player), attempt to start the single client
                        if (_status != FdNetworkStatus.SinglePlayerMenu) {
                            var levelData = Game.Instance.LoadedLevelData;
                            conn.identity.connectionToClient.Send( new StartGameMessage {
                                sessionType = SessionType.Multiplayer, 
                                levelData = levelData
                            });
                        }
                        break;
                
                    case FdNetworkStatus.LobbyMenu: 
                        LobbyPlayer lobbyPlayer = Instantiate(lobbyPlayerPrefab);
                        lobbyPlayer.isHost = LobbyPlayers.Count == 0;
            
                        NetworkServer.AddPlayerForConnection(conn, lobbyPlayer.gameObject);
                    
                        if (conn.identity != null) {
                            var player = conn.identity.GetComponent<LobbyPlayer>();
                            AddPlayer(player);
                            
                            var configPanel = FindObjectOfType<LobbyConfigurationPanel>();
                            if (configPanel) {
                                player.UpdateLobby(configPanel.LobbyLevelData);
                            }
                        }
                        break;
                    
                    case FdNetworkStatus.Offline:
                        // I don't know how the hell we get here but something gone bonkers if we do
                        Debug.LogError("Somehow tried to get a connection without being online!");
                        conn.Disconnect();
                        break;
                }
            }

            StartCoroutine(AddNewPlayerConnection());

        }
        
        // player leaves
        public override void OnServerDisconnect(NetworkConnection conn) {
            Debug.Log("[SERVER] PLAYER DISCONNECT");
                        
            // TODO: notify other players than someone has left
            if (conn.identity != null) {
                switch (_status) {
                    case FdNetworkStatus.SinglePlayerMenu:
                        var loadingPlayer = conn.identity.GetComponent<LoadingPlayer>();
                        RemovePlayer(loadingPlayer);
                        break;
                    
                    case FdNetworkStatus.LobbyMenu:
                        var lobbyPlayer = conn.identity.GetComponent<LobbyPlayer>();
                        RemovePlayer(lobbyPlayer);
                        break;  
                    
                    case FdNetworkStatus.InGame:
                        var shipPlayer = conn.identity.GetComponent<ShipPlayer>();
                        RemovePlayer(shipPlayer);
                        break;
                }
            }
            
            base.OnServerDisconnect(conn);
        }

        // Server shutdown, notify all players
        public override void OnStopClient() {
            Debug.Log("[SERVER] SHUTDOWN");
            switch (_status) {
                
                case FdNetworkStatus.LobbyMenu:
                    foreach (var lobbyPlayer in LobbyPlayers.ToArray()) {
                        lobbyPlayer.CloseLobby();
                    }
                    LobbyPlayers.Clear();
                    break;
                
                case FdNetworkStatus.Loading:
                    Game.Instance.QuitToMenu("LOST CONNECTION TO THE SERVER WHILE LOADING.");
                    LoadingPlayers.Clear();
                    break;
                
                case FdNetworkStatus.InGame:
                    Game.Instance.QuitToMenu("THE SERVER CLOSED THE CONNECTION");
                    ShipPlayers.Clear();
                    break;
                    
            }
            _status = FdNetworkStatus.Offline;
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
            }
            NetworkServer.ReplacePlayerForConnection(conn, newPlayer.gameObject, true);
            RemovePlayer(previousPlayer);
            AddPlayer(newPlayer);
            return newPlayer;
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
                    throw new Exception("Unsupported player object tyep!");
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

        // TODO: finish lobby ready state handling
        public void NotifyPlayersOfReadyState() {
            foreach (var player in LobbyPlayers) {
                player.HandleReadyStatusChanged(IsReadyToLoad());
            }
        }

        private bool IsReadyToLoad() {
            if (numPlayers < minPlayers) {
                return false; 
            }

            foreach (var player in LobbyPlayers) {
                if (!player.isReady) {
                    return false; 
                }
            }

            return true;
        }

        #endregion
        
        #region Client Message Handlers

        private void StartLoadGame(StartGameMessage message) {
            Game.Instance.StartGame(message.sessionType, message.levelData);
        }

        private void ShowLobby(ReturnToLobbyMessage message) {
            Game.Instance.QuitToLobby();
        }
        
        private void SetShipPosition(SetShipPositionMessage message) {
            var ship = ShipPlayer.FindLocal;
            if (ship) {
                ship.AbsoluteWorldPosition = message.position;
                ship.transform.rotation = message.rotation;
            }
        }
        
        #endregion
    }
}
