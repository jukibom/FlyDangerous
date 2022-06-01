using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.MapData;
using Core.OnlineServices;
using Core.Player;
using JetBrains.Annotations;
using kcp2k;
using Menus.Main_Menu.Components;
using Mirror;
using Misc;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Core.OnlineServices.SteamOnlineService;
using Mirror.FizzySteam;
using Steamworks;
#endif

namespace Core {
    public class FdNetworkManager : NetworkManager {
        public const short maxPlayerLimit = 128;

        // set to non-empty string to force password validation on connecting clients
        public static string serverPassword;

        [Header("Room")] [SerializeField] private LobbyPlayer lobbyPlayerPrefab;

        [Header("Loading")] [SerializeField] private LoadingPlayer loadingPlayerPrefab;

        [Header("In-Game")] [SerializeField] private ShipPlayer shipPlayerPrefab;

        // Mirror's transport layer IMMEDIATELY severs the connection if the internal maxConnections limit is exceeded.
        // To work around this and have some sane messaging back to clients, we're using a server limit of 129 and
        // a "true" limit of 128. The additional slot is there just to allow a client to connect, receive a message
        // explaining why they're being kicked ... and then kicked. Aren't distributed systems fun?
        public short maxPlayers = maxPlayerLimit;

        public JoinGameRequestMessage joinGameRequestMessage;

        public static FdNetworkManager Instance => singleton as FdNetworkManager;
        public List<LobbyPlayer> LobbyPlayers { get; private set; } = new();
        public List<LoadingPlayer> LoadingPlayers { get; private set; } = new();
        public List<ShipPlayer> ShipPlayers { get; private set; } = new();

        public string NetworkAddress {
            get => networkAddress;
            set => networkAddress = value;
        }

        public ushort NetworkPort {
            get => GetComponent<KcpTransport>()?.Port ?? 0;
            set => GetComponent<KcpTransport>().Port = value;
        }

        [CanBeNull] public IOnlineService OnlineService { get; private set; }
        public bool HasOnlineServices => OnlineService != null;
        public bool HasMultiplayerServices => OnlineService?.Multiplayer != null;
        public bool HasLeaderboardServices => OnlineService?.Leaderboard != null;

        // initialise steam network transport and online services if we're using steamworks
        public override void Start() {
            base.Start();
#if !DISABLESTEAMWORKS
            // use online services and steam mirror transport
            OnlineService = new SteamOnlineService();
            Destroy(GetComponent<KcpTransport>());
            Transport.activeTransport = gameObject.AddComponent<FizzySteamworks>();
            transport = Transport.activeTransport;
#endif
        }

        public static event Action<JoinGameSuccessMessage> OnClientConnected;
        public static event Action OnClientDisconnected;
        public static event Action<string> OnPlayerLeave;
        public static event Action<string> OnClientConnectionRejected;

        #region Server Message Handlers

        private void OnJoinGameServerMsg(NetworkConnection conn, JoinGameRequestMessage message) {
            IEnumerator AddNewPlayerConnection() {
                while (!conn.isReady) yield return new WaitForEndOfFrame();

                // prevent server checks against connection requests on a HostClient
                var isLocalConnection = conn.connectionId == NetworkClient.connection.connectionId;

                // authentication
                if (!isLocalConnection && !string.IsNullOrEmpty(serverPassword))
                    if (serverPassword != message.password) {
                        RejectPlayerConnection(conn, "Could not authenticate: incorrect password");
                        yield break;
                    }

                // version check
                if (!isLocalConnection)
                    if (Application.version != message.version)
                        RejectPlayerConnection(conn, "Fly Dangerous version mismatch! Server is running version " + Application.version);

                var sessionStatus = Game.Instance.SessionStatus;
                var levelData = Game.Instance.LoadedLevelData;

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
                        if (sessionStatus != SessionStatus.SinglePlayerMenu)
                            conn.identity.connectionToClient.Send(new StartGameMessage {
                                sessionType = SessionType.Multiplayer,
                                levelData = levelData
                            });
                        break;

                    case SessionStatus.LobbyMenu:
                        // Fetch host lobby config to send to client instead of game state loaded level data
                        var hostLobbyConfigurationPanel = FindObjectOfType<LobbyConfigurationPanel>();
                        if (hostLobbyConfigurationPanel) levelData = hostLobbyConfigurationPanel.LobbyLevelData;

                        var lobbyPlayer = AddPlayerFromPrefab(conn, lobbyPlayerPrefab);
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

        public struct PlayerLeaveGameMessage : NetworkMessage {
            public string playerName;
        }

        private struct StartGameMessage : NetworkMessage {
            public SessionType sessionType;
            public LevelData levelData;
        }

        private struct ReturnToLobbyMessage : NetworkMessage {
        }

        private struct SetShipPositionMessage : NetworkMessage {
            public Vector3 position;
            public Quaternion rotation;
        }

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
            var loadingPlayer = Instantiate(loadingPlayerPrefab);
            NetworkServer.AddPlayerForConnection(conn, loadingPlayer.gameObject);

            IEnumerator Reject() {
                while (!conn.isReady) yield return new WaitForEndOfFrame();
                conn.identity.connectionToClient.Send(new JoinGameRejectionMessage { reason = reason });
                yield return new WaitForEndOfFrame();

                conn.Disconnect();
            }

            StartCoroutine(Reject());
        }

        // Find the player name for a given connection - this is ONLY valid on the server!
        private string PlayerNameForConnection(NetworkConnection conn) {
            var lobbyPlayer = LobbyPlayers.Find(p => p.connectionToClient.connectionId == conn.connectionId);
            var loadingPlayer = LoadingPlayers.Find(p => p.connectionToClient.connectionId == conn.connectionId);
            var shipPlayer = ShipPlayers.Find(p => p.connectionToClient.connectionId == conn.connectionId);
            return lobbyPlayer ? lobbyPlayer.playerName : loadingPlayer ? loadingPlayer.playerName : shipPlayer ? shipPlayer.playerName : "unknown";
        }

        #endregion

        #region Start / Quit Game

        public void StartGameLoadSequence(SessionType sessionType, LevelData levelData) {
            if (NetworkServer.active) {
                // Transition any lobby players to loading state
                if (Game.Instance.SessionStatus == SessionStatus.LobbyMenu)
                    // iterate over a COPY of the lobby players (the List is mutated by transitioning!)
                    foreach (var lobbyPlayer in LobbyPlayers.ToArray())
                        TransitionToLoadingPlayer(lobbyPlayer);

                // Transition any in-game players to loading state
                if (Game.Instance.SessionStatus == SessionStatus.InGame)
                    // iterate over a COPY of the ship players (the List is mutated by transitioning!)
                    foreach (var shipPlayer in ShipPlayers.ToArray())
                        TransitionToLoadingPlayer(shipPlayer);

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
                if (Game.Instance.SessionStatus == SessionStatus.InGame) // iterate over a COPY of the lobby players (the List is mutated by transitioning!)
                    foreach (var shipPlayer in ShipPlayers.ToArray())
                        TransitionToLobbyPlayer(shipPlayer);

                // notify all clients about the new scene
                NetworkServer.SendToAll(new ReturnToLobbyMessage());
            }
            else {
                throw new Exception("Cannot return to lobby without an active server!");
            }
        }

        [Server]
        public void LoadPlayerShip(LoadingPlayer loadingPlayer) {
            // handle start position for each client
            var levelData = Game.Instance.LoadedLevelData;
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

            // if a ship player which is the host already exists and the game mode permits it, warp there instead
            var hostShip = ShipPlayers.Find(s => s.isHost);
            if (hostShip != null && Game.Instance.LoadedLevelData.gameType.CanWarpToHost) {
                position = hostShip.AbsoluteWorldPosition;
                rotation = hostShip.transform.rotation;
            }

            var ship = TransitionToShipPlayer(loadingPlayer);
            ship.ShipPhysics.gameObject.SetActive(false);

            IEnumerator SetPlayerPosition() {
                // wait once to sync positions and again to init physics, I guess? Who knows
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();

                try {
                    // TODO: radius should possibly be determined by the ship model itself!
                    position = Game.Instance.LoadedLevelData.gameType.HasFixedStartLocation
                        ? position
                        : PositionalHelpers.FindClosestEmptyPosition(position, 10);

                    // update locally immediately for subsequent collision checks
                    ship.ShipPhysics.gameObject.SetActive(true);
                    ship.AbsoluteWorldPosition = position;
                    ship.transform.rotation = rotation;

                    // ensure each client receives their assigned position
                    ship.connectionToClient.Send(new SetShipPositionMessage {
                        position = position,
                        rotation = rotation
                    });

                    // Update physics engine so subsequent collision checks are up-to-date
                    Physics.SyncTransforms();

                    // ship created and placed, notify ready (allows them to start syncing their own positions)
                    ship.ServerReady();
                }
                catch {
                    Game.Instance.QuitToMenu("The server failed to initialise properly");
                }
            }

            StartCoroutine(SetPlayerPosition());
        }

        #endregion

        #region State Management

        public void ShutdownNetwork() {
            StopAll();
            Game.Instance.SessionStatus = SessionStatus.Offline;
        }

        public void StopAll() {
            if (mode != NetworkManagerMode.Offline) {
                StopHost();
                StopClient();
            }
        }

        public override void OnApplicationQuit() {
            base.OnApplicationQuit();
#if !DISABLESTEAMWORKS
            Debug.Log("SHUTDOWN STEAM");
            SteamAPI.Shutdown();
#endif
        }

        #endregion

        #region Client Handlers

        // player joins
        public override void OnClientConnect(NetworkConnection conn) {
            Debug.Log("[CLIENT] LOCAL CLIENT HAS CONNECTED");
            base.OnClientConnect(conn);
            NetworkClient.RegisterHandler<JoinGameSuccessMessage>(OnJoinGameClientMsg);
            NetworkClient.RegisterHandler<JoinGameRejectionMessage>(OnRejectConnectionClientMsg);
            NetworkClient.RegisterHandler<PlayerLeaveGameMessage>(OnPlayerLeaveClientMsg);
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
        }

        // Server shutdown, notify all players
        public override void OnStopClient() {
            Debug.Log("[CLIENT] SERVER SHUTDOWN");

            // prevent loops with callbacks
            var sessionStatus = Game.Instance.SessionStatus;
            Game.Instance.SessionStatus = SessionStatus.Offline;
            switch (sessionStatus) {
                case SessionStatus.LobbyMenu:
                    var localPlayer = FdPlayer.FindLocalLobbyPlayer;
                    if (localPlayer != null) localPlayer.HostCloseLobby();
                    break;

                case SessionStatus.Loading:
                    Game.Instance.QuitToMenu("LOST CONNECTION TO THE SERVER WHILE LOADING.");
                    break;

                case SessionStatus.InGame:
                    Game.Instance.QuitToMenu("THE SERVER CLOSED THE CONNECTION");
                    break;
            }
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

            if (Game.Instance.SessionStatus == SessionStatus.InGame) // joining mid-game checks
                if (!Game.Instance.IsGameHotJoinable)
                    RejectPlayerConnection(conn, "Game is currently running and does not permit joining during the match.");
        }

        // player leaves
        public override void OnServerDisconnect(NetworkConnection conn) {
            Debug.Log("[SERVER] PLAYER DISCONNECT");
            NetworkServer.SendToAll(new PlayerLeaveGameMessage { playerName = PlayerNameForConnection(conn) });
            base.OnServerDisconnect(conn);
            OnClientDisconnected?.Invoke();
            UpdatePlayerLists();
        }

        public override void OnStopServer() {
            LobbyPlayers.Clear();
            LoadingPlayers.Clear();
            ShipPlayers.Clear();
        }

        #endregion

        #region Player Transition + List Management

        public void UpdatePlayerLists() {
            // player added, refresh all our internal lists
            LobbyPlayers = FindObjectsOfType<LobbyPlayer>().ToList();
            LoadingPlayers = FindObjectsOfType<LoadingPlayer>().ToList();
            ShipPlayers = FindObjectsOfType<ShipPlayer>().ToList().FindAll(shipPlayer => shipPlayer.netIdentity.isActiveAndEnabled);
        }

        private LobbyPlayer TransitionToLobbyPlayer<T>(T previousPlayer) where T : NetworkBehaviour {
            var lobbyPlayer = Instantiate(lobbyPlayerPrefab);
            lobbyPlayer.isHost = GetHostStatus(previousPlayer);
            return ReplacePlayer(lobbyPlayer, previousPlayer);
        }

        private LoadingPlayer TransitionToLoadingPlayer<T>(T previousPlayer) where T : NetworkBehaviour {
            var loadingPlayer = Instantiate(loadingPlayerPrefab);
            loadingPlayer.isHost = GetHostStatus(previousPlayer);
            return ReplacePlayer(loadingPlayer, previousPlayer);
        }

        private ShipPlayer TransitionToShipPlayer<T>(T previousPlayer) where T : NetworkBehaviour {
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
            }
            else {
                Debug.LogWarning("Null connection found when replacing player - skipping.");
            }

            UpdatePlayerLists();
            return newPlayer;
        }

        private T AddPlayerFromPrefab<T>(NetworkConnection conn, T prefab) where T : NetworkBehaviour {
            var playerConnectionPrefab = Instantiate(prefab);
            NetworkServer.AddPlayerForConnection(conn, playerConnectionPrefab.gameObject);
            if (conn.identity != null) {
                var player = conn.identity.GetComponent<T>();
                UpdatePlayerLists();
                return player;
            }

            throw new Exception("Failed to add player with null connection!");
        }

        private bool GetHostStatus<T>(T player) where T : NetworkBehaviour {
            if (player != null)
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

            return false;
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

        private void OnPlayerLeaveClientMsg(PlayerLeaveGameMessage message) {
            Debug.Log($"[CLIENT] Player {message.playerName} left the game");
            OnPlayerLeave?.Invoke(message.playerName);
        }

        private void OnStartLoadGameClientMsg(StartGameMessage message) {
            Game.Instance.StartGame(message.sessionType, message.levelData);
        }

        private void OnShowLobbyClientMsg(ReturnToLobbyMessage message) {
            Game.Instance.QuitToLobby();
        }

        private void OnSetShipPositionClientMsg(SetShipPositionMessage message) {
            // this may be received before the ship has finished loading - wait until it's available.
            IEnumerator SetShipPosition(SetShipPositionMessage positionMessage) {
                var ship = FdPlayer.FindLocalShipPlayer;

                yield return new WaitUntil(() => {
                    ship = FdPlayer.FindLocalShipPlayer;
                    return ship != null;
                });

                ship.AbsoluteWorldPosition = positionMessage.position;
                ship.transform.rotation = positionMessage.rotation;

                // instantly warp camera to start position rather than damping rotate
                ship.User.ShipCameraRig.Reset();
            }

            StartCoroutine(SetShipPosition(message));
        }

        #endregion
    }
}