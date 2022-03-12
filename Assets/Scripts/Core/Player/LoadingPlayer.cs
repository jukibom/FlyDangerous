using Core.MapData;
using Mirror;

namespace Core.Player {
    public class LoadingPlayer : FdPlayer {
        [SyncVar] public string playerName;

        [SyncVar] public bool isHost;

        [field: SyncVar] public bool IsLoaded { get; private set; }

        private void Start() {
            // We want to keep this around when jumping to the loading scene and manually destroy it later.
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() {
            LevelLoader.OnLevelLoaded += OnLevelLoaded;
        }

        private void OnDisable() {
            LevelLoader.OnLevelLoaded -= OnLevelLoaded;
        }

        // On local client start
        public override void OnStartAuthority() {
            CmdSetPlayerName(Preferences.Instance.GetString("playerName"));
        }

        public void RequestTransitionToShipPlayer() {
            CmdRequestTransitionToShipPlayer();
        }

        private void OnLevelLoaded() {
            // store loaded state, inform network layer
            IsLoaded = true;
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