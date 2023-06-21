using Mirror;

namespace Core.Player {
    /// <summary>
    /// Represents a player during the game's loading process, inheriting from the FdPlayer class. Contains syncing variables
    /// for player name, host status, and loading status.
    /// </summary>
    public class LoadingPlayer : FdPlayer {
        [SyncVar] public string playerName;

        [SyncVar] public bool isHost;

        [SyncVar] public bool isLoaded;

        private void Start() {
            // We want to keep this around when jumping to the loading scene and manually destroy it later.
            DontDestroyOnLoad(gameObject);
            isLoaded = false;
        }

        /// <summary>
        /// Called when the local player object has been set up on this client. Sets the game object's tag to indicate its role in terrain
        /// generation focus.
        /// </summary>
        public override void OnStartLocalPlayer() {
            // set tag for terrain generation focus
            gameObject.tag = "Terrain Gen Marker";
        }

        /// <summary>
        /// On local client start set the name
        /// </summary>
        public override void OnStartAuthority() {
            CmdSetPlayerName(Misc.Player.LocalPlayerName);
        }

        /// <summary>
        /// Requests a transition from the LoadingPlayer state to the ShipPlayer state by invoking the CmdRequestTransitionToShipPlayer command.
        /// </summary>
        public void RequestTransitionToShipPlayer() {
            CmdRequestTransitionToShipPlayer();
        }


        /// <summary>
        /// Shows the loading room for the local client instance only by attaching the LoadingRoom object to the current player's transform.
        /// </summary>
        public void ShowLoadingRoom() {
            // show the loading camera, geo etc. This should only apply to local client instance!
            var loadingRoom = FindObjectOfType<LoadingRoom>();
            if (loadingRoom) loadingRoom.transform.SetParent(transform, false);
        }

        /// <summary>
        /// Sets this LoadingPlayer instance as the focus of the FloatingOrigin. This causes the FloatingOrigin to update its position
        /// and rotation based on this instance's transform.
        /// </summary>
        public void SetFloatingOrigin() {
            FloatingOrigin.Instance.FocalTransform = transform;
            FloatingOrigin.Instance.ForceUpdate();
        }

        /// <summary>
        /// Sets the current LoadingPlayer instance into the loaded state by calling the CmdSetIsLoaded command.
        /// </summary>
        public void SetLoaded() {
            CmdSetIsLoaded();
        }

        [Command]
        private void CmdSetIsLoaded() {
            isLoaded = true;
        }

        [Command]
        private void CmdSetPlayerName(string name) {
            if (name == "") name = "UNNAMED SCRUB";

            playerName = name;
        }

        [Command]
        private void CmdRequestTransitionToShipPlayer() {
            FdNetworkManager.Instance.LoadPlayerShip(this);
        }
    }
}