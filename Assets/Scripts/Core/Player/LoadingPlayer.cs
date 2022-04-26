using Mirror;

namespace Core.Player {
    public class LoadingPlayer : FdPlayer {
        [SyncVar] public string playerName;

        [SyncVar] public bool isHost;

        [SyncVar] public bool isLoaded;

        private void Start() {
            // We want to keep this around when jumping to the loading scene and manually destroy it later.
            DontDestroyOnLoad(gameObject);
            isLoaded = false;
        }

        public override void OnStartLocalPlayer() {
            // set tag for terrain generation focus
            gameObject.tag = "Terrain Gen Marker";
        }

        // On local client start
        public override void OnStartAuthority() {
            CmdSetPlayerName(Misc.Player.LocalPlayerName);
        }

        public void RequestTransitionToShipPlayer() {
            CmdRequestTransitionToShipPlayer();
        }

        // show the loading camera, geo etc. This should only apply to local client instance!
        public void ShowLoadingRoom() {
            var loadingRoom = FindObjectOfType<LoadingRoom>();
            if (loadingRoom) loadingRoom.transform.SetParent(transform, false);
        }

        // register self as floating origin focus
        public void SetFloatingOrigin() {
            FloatingOrigin.Instance.FocalTransform = transform;
            FloatingOrigin.Instance.ForceUpdate();
        }

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