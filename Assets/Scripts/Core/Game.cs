using System.Collections;
using System.Linq;
using System.Reflection;
using Cinemachine;
using Core.MapData;
using Core.Player;
using Core.ShipModel;
using Gameplay;
using JetBrains.Annotations;
using MapMagic.Core;
using Menus.Main_Menu;
using Mirror;
using Misc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Management;
using Environment = System.Environment;

namespace Core {
    public enum SessionType {
        Singleplayer,
        Multiplayer
    }

    public enum SessionStatus {
        Development,
        Offline,
        SinglePlayerMenu,
        LobbyMenu,
        Loading,
        InGame
    }

    public class Game : Singleton<Game> {
        public delegate void GamePauseAction(bool enabled);

        public delegate void GameSettingsApplyAction();

        public delegate void PlayerJoinAction();

        public delegate void PlayerLeaveAction();

        public delegate void RestartLevelAction();

        public delegate void VRToggledAction(bool enabled);

        private static readonly int fadeToBlack = Animator.StringToHash("FadeToBlack");
        private static readonly int fadeFromBlack = Animator.StringToHash("FadeFromBlack");

        [SerializeField] private InputActionAsset playerBindings;
        [SerializeField] private ScriptableRendererFeature ssao;
        [SerializeField] private Camera inGameUiCamera;
        [SerializeField] private Animator crossfade;

        private CinemachineBrain _cinemachine;
        private Vector3 _hmdPosition;
        private Quaternion _hmdRotation;
        private LevelLoader _levelLoader;
        private Coroutine _loadingRoutine;
        private ShipParameters _shipParameters;

        // The level data most recently used to load a map
        public LevelData LoadedLevelData => _levelLoader.LoadedLevelData;

        // The level data hydrated with the current player position and track layout
        public LevelData LevelDataAtCurrentPosition => _levelLoader.LevelDataAtCurrentPosition;

        public SessionType SessionType { get; private set; } = SessionType.Singleplayer;
        public SessionStatus SessionStatus { get; set; } = SessionStatus.Offline;

        public bool IsVREnabled { get; private set; }

        public bool IsGameHotJoinable => LoadedLevelData.gameType.IsHotJoinable;

        public ShipParameters ShipParameters {
            get {
                if (_shipParameters != null) return _shipParameters;
                _shipParameters = ShipParameters.Defaults;
                var player = FdPlayer.FindLocalShipPlayer;
                if (player != null) _shipParameters = player.ShipPhysics.CurrentParameters;
                return _shipParameters;
            }
            set {
                _shipParameters = value;
                var ship = FdPlayer.FindLocalShipPlayer;
                if (ship) ship.ShipPhysics.CurrentParameters = _shipParameters;
            }
        }

        public Camera InGameUICamera => inGameUiCamera;

        public bool IsTerrainMap => _levelLoader.LoadedLevelData.location.IsTerrain;

        public string Seed => _levelLoader.LoadedLevelData.terrainSeed;

        // show certain things if first time hitting the menu
        public bool MenuFirstRun { get; private set; } = true;

        public void Start() {
            // must be a cinemachine controller in the scene
            _cinemachine = FindObjectOfType<CinemachineBrain>();

            // must be a level loader in the scene
            _levelLoader = FindObjectOfType<LevelLoader>();

            // if there's a user object when the game starts, enable input (usually in the editor!)
            FindObjectOfType<ShipPlayer>()?.User.EnableGameInput();
            LoadBindings();
            ApplyGameOptions();

            // We use a custom canvas cursor to work in VR and pancake
            Cursor.visible = false;

            // check for command line args
            var args = Environment.GetCommandLineArgs();
            if (args.ToList().Contains("-vr") || args.ToList().Contains("-VR")) EnableVR();

            // Subscribe to network events
            FdNetworkManager.OnClientDisconnected += () => OnPlayerLeave?.Invoke();

            // load hmd position from preferences
            _hmdPosition = Preferences.Instance.GetVector3("hmdPosition");
            _hmdRotation = Quaternion.Euler(Preferences.Instance.GetVector3("hmdRotation"));
        }

        private void OnDestroy() {
            DisableVRIfNeeded();
        }

        private void OnApplicationQuit() {
            DisableVRIfNeeded();
        }

        public static event PlayerJoinAction OnPlayerLoaded;
        public static event PlayerLeaveAction OnPlayerLeave;
        public static event RestartLevelAction OnRestart;
        public static event GameSettingsApplyAction OnGameSettingsApplied;
        public static event GamePauseAction OnPauseToggle;
        public static event VRToggledAction OnVRStatus;

        public void LoadBindings() {
            var bindings = Preferences.Instance.GetString("inputBindings");
            if (!string.IsNullOrEmpty(bindings)) playerBindings.LoadBindingOverridesFromJson(bindings);
        }

        public void ApplyGameOptions() {
            if (OnGameSettingsApplied != null) OnGameSettingsApplied();
            ApplyGraphicsOptions();
        }

        private void ApplyGraphicsOptions() {
            var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            urp.renderScale = Preferences.Instance.GetFloat("graphics-render-scale");

            // For some maddening reason soft shadows is not exposed by flipping this bool does work so here's some awful reflection. yay!
            var type = urp.GetType();
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            var fInfo = type.GetField("m_SoftShadowsSupported", bindingFlags);
            if (fInfo != null) fInfo.SetValue(urp, Preferences.Instance.GetBool("graphics-soft-shadows"));

            var msaa = Preferences.Instance.GetString("graphics-anti-aliasing");
            switch (msaa) {
                case "8x":
                    urp.msaaSampleCount = 8;
                    break;
                case "4x":
                    urp.msaaSampleCount = 4;
                    break;
                case "2x":
                    urp.msaaSampleCount = 2;
                    break;
                case "none":
                case "default":
                    urp.msaaSampleCount = 0;
                    break;
            }

            ssao.SetActive(Preferences.Instance.GetBool("graphics-ssao"));
        }

        public void EnableVR() {
            IEnumerator StartXR() {
                yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
                XRGeneralSettings.Instance.Manager.StartSubsystems();
                IsVREnabled = true;
                NotifyVRStatus();
            }

            StartCoroutine(StartXR());
        }

        public void DisableVRIfNeeded() {
            if (IsVREnabled) {
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
                IsVREnabled = false;
                NotifyVRStatus();
            }
        }

        public void ResetHmdView(XRRig xrRig, Transform targetTransform) {
            xrRig.MoveCameraToWorldLocation(targetTransform.position);
            xrRig.MatchRigUpCameraForward(targetTransform.up, targetTransform.forward);

            var xrRigTransform = xrRig.transform;
            _hmdPosition = xrRigTransform.localPosition;
            _hmdRotation = xrRigTransform.localRotation;

            Preferences.Instance.SetVector3("hmdPosition", _hmdPosition);
            Preferences.Instance.SetVector3("hmdRotation", _hmdRotation.eulerAngles);
            Preferences.Instance.Save();
        }

        public void StartGame(SessionType sessionType, LevelData levelData) {
            /* Split this into single and multiplayer - logic should be mostly the same but we don't
                transition from a lobby and we need to set the queryable SessionType for other logic in-game
                (e.g. no actual pause on pause menu, quick to menu being quit to lobby etc)
            */
            SessionType = sessionType;
            SessionStatus = SessionStatus.Loading;

            LockCursor();

            IEnumerator WaitForAllPlayersLoaded() {
                yield return FindObjectsOfType<LoadingPlayer>().All(loadingPlayer => loadingPlayer.IsLoaded)
                    ? null
                    : new WaitForFixedUpdate();
            }

            IEnumerator LoadGame() {
                yield return _levelLoader.ShowLoadingScreen();

                // Position the active camera to the designated start location so we can be sure to load in anything
                // important at that location as part of the load sequence 
                var loadingRoom = FindObjectOfType<LoadingRoom>();
                if (loadingRoom) {
                    var loadingPlayerCameraTransform = loadingRoom.transform;
                    loadingPlayerCameraTransform.position = levelData.startPosition.ToVector3();
                }

                yield return _levelLoader.StartGame(levelData);

                // wait for all known currently loading players to have finished loading
                // TODO: show "Waiting for Players" text in loading screen
                yield return WaitForAllPlayersLoaded();

                var loadingPlayer = FdPlayer.FindLocalLoadingPlayer;
                if (loadingPlayer) {
                    loadingPlayer.RequestTransitionToShipPlayer();
                }
                else {
                    QuitToMenu("Failed to create connection");
                    yield break;
                }

                SessionStatus = SessionStatus.InGame;

                // wait for local ship client object
                while (!FdPlayer.FindLocalShipPlayer) {
                    Debug.Log("Session loaded, waiting for player init");
                    yield return new WaitForEndOfFrame();
                }

                var ship = FdPlayer.FindLocalShipPlayer;

                // Allow the rigid body to initialise before setting new parameters!
                yield return new WaitForEndOfFrame();

                ship.ShipPhysics.CurrentParameters = ShipParameters;
                var shipPosition = ship.transform.position;
                _levelLoader.LoadedLevelData.startPosition = LevelDataVector3.FromVector3(shipPosition);

                // set up graphics settings (e.g. camera FoV) + VR status (cameras, radial fog etc)
                ApplyGameOptions();
                NotifyVRStatus();

                yield return _levelLoader.HideLoadingScreen();

                // if there's a track, initialise it
                var track = FindObjectOfType<Track>();
                if (track) track.InitialiseTrack();

                // resume the game
                Time.timeScale = 1;
                SetFlatScreenCameraControllerActive(!IsVREnabled);
                FadeFromBlack();
                yield return new WaitForSeconds(0.7f);

                // if there's a track in the game world, start it
                if (track) yield return track.StartTrackWithCountdown();

                // enable user input
                ship.User.EnableGameInput();

                // notify other players for e.g. targeting systems
                ship.CmdNotifyPlayerLoaded();
            }

            _loadingRoutine = StartCoroutine(LoadGame());
        }

        public void RestartSession() {
            StartCoroutine(_levelLoader.RestartLevel(() => {
                if (OnRestart != null) OnRestart();
            }));
        }

        // Graceful leave game and decide if to transition back to lobby
        public void LeaveSession() {
            if (SessionType == SessionType.Multiplayer && NetworkClient.isHostClient)
                FdNetworkManager.Instance.StartReturnToLobbySequence();
            else
                QuitToMenu();
        }

        // Drop back to the menu but retain the network connection and transition players back to the lobby
        public void QuitToLobby() {
            IEnumerator ReturnPlayersToLobby() {
                yield return LoadMainMenu();
                var mainMenu = FindObjectOfType<MainMenu>();
                mainMenu.ShowLobby();
            }

            SessionStatus = SessionStatus.LobbyMenu;
            StartCoroutine(ReturnPlayersToLobby());
        }

        // Drop back to the menu (with optional disconnection reason) - this will destroy the active network session
        // for all current users (ideally gracefully for clients not expecting it!)
        // This should be used in ALL cases where disconnection has unexpectedly occurred or where network services
        // are no longer required (graceful client quit)
        public void QuitToMenu([CanBeNull] string withDisconnectionReason = null) {
            // save any pending preferences (e.g. mouselook, camera etc)
            Preferences.Instance.Save();

            if (!FindObjectOfType<MainMenu>()) {
                IEnumerator QuitAndShutdownNetwork() {
                    yield return LoadMainMenu(withDisconnectionReason);
                    SessionStatus = SessionStatus.Offline;
                    FdNetworkManager.Instance.StopAll();
                }

                StartCoroutine(QuitAndShutdownNetwork());
            }
        }

        private IEnumerator LoadMainMenu([CanBeNull] string withDisconnectionReason = null) {
            if (_loadingRoutine != null) StopCoroutine(_loadingRoutine);

            MenuFirstRun = false;
            var mapMagic = FindObjectOfType<MapMagicObject>();
            if (mapMagic) {
                mapMagic.StopGenerate();
                mapMagic.enabled = false;
            }

            var ship = FdPlayer.FindLocalShipPlayer;
            if (ship) ship.User.DisableGameInput();

            IEnumerator LoadMenuScene() {
                // during load we pause scaled time to prevent *absolutely anything* from interacting incorrectly
                Time.timeScale = 1;

                FadeToBlack();
                yield return new WaitForSeconds(0.5f);
                yield return SceneManager.LoadSceneAsync("Main Menu");
                SetFlatScreenCameraControllerActive(false);
                yield return new WaitForEndOfFrame();
                ApplyGameOptions();
                yield return new WaitForEndOfFrame();
                NotifyVRStatus();
                FreeCursor();

                var mainMenu = FindObjectOfType<MainMenu>();
                if (mainMenu && withDisconnectionReason != null) mainMenu.ShowDisconnectedDialog(withDisconnectionReason);
            }

            yield return LoadMenuScene();
        }

        public void QuitGame() {
            IEnumerator Quit() {
                FadeToBlack();
                yield return new WaitForSeconds(0.5f);
                Application.Quit();
            }

            StartCoroutine(Quit());
        }

        public void FreeCursor() {
            Cursor.lockState = CursorLockMode.None;
        }

        public void LockCursor() {
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void FadeToBlack() {
            crossfade.SetTrigger(fadeToBlack);
        }

        public void FadeFromBlack() {
            crossfade.SetTrigger(fadeFromBlack);
        }

        public void PauseGameToggle(bool paused) {
            if (OnPauseToggle != null) OnPauseToggle(paused);

            if (paused) {
                // actual game logic pause only applies to single player
                if (SessionType == SessionType.Singleplayer) Time.timeScale = 0;
                FreeCursor();
            }
            else {
                Time.timeScale = 1;
                LockCursor();
            }
        }

        public void SetFlatScreenCameraControllerActive(bool active) {
            _cinemachine.gameObject.SetActive(active);
        }

        private void NotifyVRStatus() {
            if (OnVRStatus != null) OnVRStatus(IsVREnabled);

            // if user has previously applied a HMD position, reapply
            if (IsVREnabled) {
                IEnumerator ResetHmdPosition() {
                    // allow xr rigs to be initialised before resetting hmd position
                    yield return new WaitForEndOfFrame();
                    var xrRig = FindObjectOfType<XRRig>(true);
                    if (xrRig) {
                        var xrTransform = xrRig.transform;

                        _hmdPosition = Preferences.Instance.GetVector3("hmdPosition");
                        _hmdRotation = Quaternion.Euler(Preferences.Instance.GetVector3("hmdRotation"));

                        xrTransform.localRotation = _hmdRotation;
                        xrTransform.localPosition = _hmdPosition;
                    }
                }

                StartCoroutine(ResetHmdPosition());
            }
        }

        public void NotifyPlayerLoaded() {
            OnPlayerLoaded?.Invoke();
        }
    }
}