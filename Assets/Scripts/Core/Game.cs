using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Audio;
using Cinemachine;
using Core.MapData;
using Core.Player;
using Core.Replays;
using Core.ShipModel;
using Gameplay;
using JetBrains.Annotations;
using MapMagic.Core;
using Menus.Main_Menu;
using Mirror;
using Misc;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Management;
using Environment = System.Environment;
#if !NO_PAID_ASSETS
using GPUInstancer;
#endif

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

        public delegate void GhostAddedAction();

        public delegate void GhostRemovedAction();

        public delegate void PlayerJoinAction();

        public delegate void PlayerLeaveAction();

        public delegate void RestartLevelAction();

        public delegate void VRToggledAction(bool enabled);

        [SerializeField] private InputActionAsset playerBindings;
        [SerializeField] private ScriptableRendererFeature ssao;
        [SerializeField] private Camera inGameUiCamera;
        [SerializeField] private CrossFade crossfade;
        [SerializeField] private ShipGhost shipGhostPrefab;

        private CinemachineBrain _cinemachine;
        private Vector3 _hmdPosition;
        private Quaternion _hmdRotation;
        private LevelLoader _levelLoader;
        private Coroutine _loadingRoutine;
        private Coroutine _sessionRestartCoroutine;
        private ShipParameters _shipParameters;

        [CanBeNull] public Level loadedMainLevel;

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

        public List<Replay> ActiveGameReplays { get; set; } = new();

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
            FdNetworkManager.OnPlayerLeave += _ => OnPlayerLeave?.Invoke();

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

        public static event PlayerJoinAction OnPlayerJoin;
        public static event PlayerLeaveAction OnPlayerLeave;
        public static event GhostAddedAction OnGhostAdded;
        public static event GhostRemovedAction OnGhostRemoved;
        public static event RestartLevelAction OnRestart;
        public static event GameSettingsApplyAction OnGameSettingsApplied;
        public static event GamePauseAction OnPauseToggle;
        public static event VRToggledAction OnVRStatus;

        public void LoadBindings() {
            var bindings = Preferences.Instance.GetString("inputBindings");
            if (bindings != null) playerBindings.LoadBindingOverridesFromJson(bindings);
        }

        public void ApplyGameOptions() {
            OnGameSettingsApplied?.Invoke();
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

            // reflections
            var shipPlayer = FdPlayer.FindLocalShipPlayer;
            if (shipPlayer) {
                var reflectionSetting = Preferences.Instance.GetString("graphics-reflections");
                shipPlayer.ReflectionProbe.enabled = reflectionSetting != "off";
                switch (reflectionSetting) {
                    case "high":
                        shipPlayer.ReflectionProbe.resolution = 512;
                        shipPlayer.ReflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                        break;
                    case "medium":
                        shipPlayer.ReflectionProbe.resolution = 256;
                        shipPlayer.ReflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                        break;
                    default: // low and any other string value
                        shipPlayer.ReflectionProbe.resolution = 128;
                        shipPlayer.ReflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                        break;
                }
            }

            // fps cap
            var fpsCap = Preferences.Instance.GetFloat("graphics-fps-cap");
            Application.targetFrameRate = Convert.ToInt32(fpsCap);
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
                yield return new WaitUntil(() => FindObjectsOfType<LoadingPlayer>().All(loadingPlayer => loadingPlayer.isLoaded));
            }

            IEnumerator LoadGame() {
                yield return _levelLoader.ShowLoadingScreen();

                // Position the active camera to the designated start location so we can be sure to load in anything
                // important at that location as part of the load sequence
                var loadingPlayer = FdPlayer.FindLocalLoadingPlayer;

                yield return new WaitUntil(() => {
                    loadingPlayer = FdPlayer.FindLocalLoadingPlayer;
                    return loadingPlayer != null;
                });

                loadingPlayer.ShowLoadingRoom();
                loadingPlayer.transform.position = levelData.startPosition.ToVector3();

                yield return _levelLoader.StartGame(levelData);

                // wait for all known currently loading players to have finished loading
                loadingPlayer.SetLoaded();
                var loadText = GameObject.FindGameObjectWithTag("DynamicLoadingText").GetComponent<Text>();
                loadText.text = "Waiting for all players to load ...";
                yield return WaitForAllPlayersLoaded();

                loadingPlayer.RequestTransitionToShipPlayer();

                SessionStatus = SessionStatus.InGame;

                // wait for local ship client object
                while (!FdPlayer.FindLocalShipPlayer) {
                    Debug.Log("Session loaded, waiting for player init");
                    yield return new WaitForEndOfFrame();
                }

                var ship = FdPlayer.FindLocalShipPlayer;

                // Allow the rigid body to initialise before setting new parameters!
                yield return new WaitForEndOfFrame();

                // set up graphics settings (e.g. camera FoV)
                ApplyGameOptions();

#if !NO_PAID_ASSETS
                // gpu instancer VR initialisation (paid asset!)
                if (IsVREnabled) {
                    var cam = FindObjectOfType<XRRig>(true).cameraGameObject.GetComponent<Camera>();

                    var gpuInstancer = FindObjectOfType<GPUInstancerMapMagic2Integration>();
                    var mapMagic = FindObjectOfType<MapMagicObject>();
                    if (mapMagic && gpuInstancer) {
                        gpuInstancer.floatingOriginTransform = mapMagic.transform;
                        GPUInstancerAPI.SetCamera(cam);
                        gpuInstancer.SetCamera(cam);
                        FindObjectsOfType<GPUInstancerDetailManager>(true).ToList().ForEach(manager => manager.SetCamera(cam));
                        FindObjectOfType<GPUInstancerTreeManager>(true)?.SetCamera(cam);
                    }
                }

                // pull out GPU instancer tree manager object before loading screen is destroyed 
                var treeManager = FindObjectOfType<GPUInstancerTreeManager>();
                var instancer = FindObjectOfType<GPUInstancerMapMagic2Integration>();
                if (treeManager && instancer) treeManager.transform.parent = instancer.transform;
#endif

                yield return _levelLoader.HideLoadingScreen();

                // set the game mode
                ship.User.inGameUI.GameModeUIHandler.SetGameMode(LoadedLevelData.gameType);

                // if there's a track, initialise it
                var track = FindObjectOfType<Track>();
                if (track) track.InitialiseTrack();

                ship.ShipPhysics.CurrentParameters = ShipParameters;

                // resume the game
                if (LoadedLevelData.gameType == GameType.Training) Time.timeScale = 0.5f;
                else Time.timeScale = 1;
                SetFlatScreenCameraControllerActive(!IsVREnabled);

                // notify VR status (e.g. setting canvas world space, cameras, radial fog etc)
                NotifyVRStatus();

                FdConsole.Instance.LogMessage("Loaded level " + levelData.LevelHash());

                FadeFromBlack();
                yield return new WaitForSeconds(0.7f);

                // if there's a track in the game world, start it
                if (track) yield return track.StartTrackWithCountdown();

                // store the starting position after any correction (e.g. move above terrain height)
                // TODO: What impact, if any, does this have on level hashes??
                var shipPosition = ship.AbsoluteWorldPosition;
                _levelLoader.LoadedLevelData.startPosition = LevelDataVector3.FromVector3(shipPosition);

                // enable user input
                ship.User.EnableGameInput();

                // notify other players for e.g. targeting systems
                ship.CmdNotifyPlayerLoaded();
            }

            _loadingRoutine = StartCoroutine(LoadGame());
        }

        public void RestartSession() {
            // this seems a little hairy but the loading routine includes the countdown and restart can only happen once the game has loaded :|
            if (_loadingRoutine != null) StopCoroutine(_loadingRoutine);
            if (_sessionRestartCoroutine != null) StopCoroutine(_sessionRestartCoroutine);
            _sessionRestartCoroutine = StartCoroutine(_levelLoader.RestartLevel(() => { OnRestart?.Invoke(); }));
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
                MusicManager.Instance.StopMusic(true);
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
            crossfade.FadeToBlack();
        }

        public void FadeFromBlack() {
            crossfade.FadeFromBlack();
        }

        public void PauseGameToggle(bool paused) {
            OnPauseToggle?.Invoke(paused);

            if (paused) {
                // actual game logic pause only applies to single player
                if (SessionType == SessionType.Singleplayer) Time.timeScale = 0;
                FreeCursor();
            }
            else {
                if (LoadedLevelData.gameType == GameType.Training) Time.timeScale = 0.5f;
                else Time.timeScale = 1;
                LockCursor();
            }
        }

        public void SetFlatScreenCameraControllerActive(bool active) {
            _cinemachine.gameObject.SetActive(active);
        }

        public void NotifyVRStatus() {
            OnVRStatus?.Invoke(IsVREnabled);

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

            // Handle global LOD bias for VR camera FOV (see https://forum.unity.com/threads/lodgroup-in-vr.455394/#post-2952522)
            var lodBias = IsVREnabled ? 3.46f : 1;
            QualitySettings.SetLODSettings(lodBias, 0);
        }

        public ShipGhost LoadGhost(Replay replay) {
            var ghost = Instantiate(shipGhostPrefab);
            OnGhostAdded?.Invoke();
            ghost.LoadReplay(replay);
            ghost.ReplayTimeline.Play();
            return ghost;
        }

        public void RemoveGhost(ShipGhost shipGhost) {
            // we have to wait a frame for all deletions to occur before firing any events
            IEnumerator DestroyGhost() {
                shipGhost.ReplayTimeline.Stop();
                var replayObject = shipGhost.ReplayTimeline.ShipReplayObject;
                if (replayObject != null) Destroy(replayObject.Transform.gameObject);
                Destroy(shipGhost.gameObject);

                yield return new WaitForEndOfFrame();
                OnGhostRemoved?.Invoke();
            }

            StartCoroutine(DestroyGhost());
        }

        public void NotifyPlayerLoaded() {
            OnPlayerJoin?.Invoke();
        }
    }
}