using JetBrains.Annotations;
using Mirror;

namespace Core.Player {
    public class FdPlayer : NetworkBehaviour {
        private static LobbyPlayer _localLobbyPlayer;
        private static LoadingPlayer _localLoadingPlayer;
        private static ShipPlayer _localShipPlayer;

        [CanBeNull]
        public static LobbyPlayer FindLocalLobbyPlayer {
            get {
                return _localLobbyPlayer
                    ? _localLobbyPlayer
                    : _localLobbyPlayer = FdNetworkManager.Instance.LobbyPlayers.Find(
                        lobbyPlayer => lobbyPlayer.isLocalPlayer);
            }
        }

        [CanBeNull]
        public static LoadingPlayer FindLocalLoadingPlayer {
            get {
                return _localLoadingPlayer
                    ? _localLoadingPlayer
                    : _localLoadingPlayer = FdNetworkManager.Instance.LoadingPlayers.Find(
                        loadingPlayer => loadingPlayer.isLocalPlayer);
            }
        }

        [CanBeNull]
        public static ShipPlayer FindLocalShipPlayer {
            get {
                return _localShipPlayer
                    ? _localShipPlayer
                    : _localShipPlayer = FdNetworkManager.Instance.ShipPlayers.Find(
                        shipPlayer => shipPlayer.isLocalPlayer);
            }
        }

        public override void OnStartClient() {
            if (isLocalPlayer) CmdNotifyPlayerLoaded();
        }

        [Command]
        public void CmdNotifyPlayerLoaded() {
            RpcNotifyPlayerLoaded();
        }

        [ClientRpc]
        private void RpcNotifyPlayerLoaded() {
            FdNetworkManager.Instance.UpdatePlayerLists();
            Game.Instance.NotifyPlayerLoaded();
        }
    }
}