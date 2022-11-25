using System;
using System.Collections;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;

namespace Core.Player {
    public class FdPlayer : NetworkBehaviour {
        private static LobbyPlayer _localLobbyPlayer;
        private static LoadingPlayer _localLoadingPlayer;
        private static ShipPlayer _localShipPlayer;
        
        #region public getters, these will throw if null so be careful!
        
        [NotNull] public static LobbyPlayer LocalLobbyPlayer {
            get {
                if (_localLobbyPlayer == null) {
                    throw new Exception("Attempted to access null local lobby player!");
                }
                return _localLobbyPlayer;
            }
        }
        
        [NotNull] public static LoadingPlayer LocalLoadingPlayer {
            get {
                if (_localLoadingPlayer == null) {
                    throw new Exception("Attempted to access null local loading player!");
                }
                return _localLoadingPlayer;
            }
        }
        
        [NotNull] public static ShipPlayer LocalShipPlayer {
            get {
                if (_localShipPlayer == null) {
                    throw new Exception("Attempted to access null local ship player!");
                }
                return _localShipPlayer;
            }
        }
        
        #endregion

        #region finders, these may be null so check on return 
        
        [CanBeNull]
        public static LobbyPlayer FindLocalLobbyPlayer {
            get {
                return _localLobbyPlayer != null
                    ? _localLobbyPlayer
                    : _localLobbyPlayer = FdNetworkManager.Instance.LobbyPlayers.Find(
                        lobbyPlayer => lobbyPlayer.isLocalPlayer);
            }
        }

        [CanBeNull]
        public static LoadingPlayer FindLocalLoadingPlayer {
            get {
                return _localLoadingPlayer != null
                    ? _localLoadingPlayer
                    : _localLoadingPlayer = FdNetworkManager.Instance.LoadingPlayers.Find(
                        loadingPlayer => loadingPlayer.isLocalPlayer);
            }
        }

        [CanBeNull]
        public static ShipPlayer FindLocalShipPlayer {
            get {
                return _localShipPlayer != null
                    ? _localShipPlayer
                    : _localShipPlayer = FdNetworkManager.Instance.ShipPlayers.Find(
                        shipPlayer => shipPlayer.isLocalPlayer);
            }
        }
        
        #endregion

        #region waiters, once returned the static getter will be valid
        
        public static IEnumerator WaitForLobbyPlayer() {
            yield return new WaitUntil(() => FindLocalLobbyPlayer != null);
        }
        
        public static IEnumerator WaitForLoadingPlayer() {
            yield return new WaitUntil(() => FindLocalLoadingPlayer != null);
        }
        
        public static IEnumerator WaitForShipPlayer() {
            yield return new WaitUntil(() => FindLocalShipPlayer != null);
        }
        
        #endregion

        #region network notifiers
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
        #endregion
    }
}