using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Audio;
using Cinemachine;
using Core.MapData;
using Core.MapData.Serializable;
using Core.Player;
using Core.Replays;
using Core.ShipModel;
using Core.ShipModel.Modifiers.Water;
using FdUI;
using Gameplay;
using Gameplay.Game_Modes;
using JetBrains.Annotations;
using MapMagic.Core;
using Menus.Main_Menu;
using Mirror;
using Misc;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
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

    [RequireComponent(typeof(GameModeHandler))]
    public class Game : Singleton<Game> {
        public delegate void GamePauseAction(bool enabled);

        public delegate void GameSettingsApplyAction();

        public delegate void GhostAddedAction();

        public delegate void GhostRemovedAction();

        public delegate void PlayerJoinAction();

        public delegate void PlayerLeaveAction();

        public delegate void RestartLevelAction();

        public delegate void WaterTransition(bool isSubmerged, Vector3 force);

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

        public bool InGame => SessionStatus is SessionStatus.InGame or SessionStatus.Development;
        public GameModeHandler GameModeHandler { get; private set; }

        public static bool IsVREnabled { get; private set; }
        public static bool IsUnderWater { get; private set; }
        public static bool IsAprilFools => DateTime.Now is { Day: 1, Month: 4 };

        public bool IsGameHotJoinable => LoadedLevelData.gameType.GameMode.IsHotJoinable;

        public ShipParameters ShipParameters {
            get {
                if (_shipParameters != null) return _shipParameters;
                _shipParameters = ShipParameters.Defaults;
                var player = FdPlayer.FindLocalShipPlayer;
                if (player != null) _shipParameters = player.ShipPhysics.FlightParameters;
                return _shipParameters;
            }
            set {
                _shipParameters = value;
                var ship = FdPlayer.FindLocalShipPlayer;
                if (ship != null) ship.ShipPhysics.FlightParameters = _shipParameters;
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

            // bootstrap for various game mode types
            GameModeHandler = GetComponent<GameModeHandler>();

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
        public static event WaterTransition OnWaterTransition;

        public void LoadBindings() {
            var bindings = Preferences.Instance.GetString("inputBindings");
            if (bindings != null) playerBindings.LoadBindingOverridesFromJson(bindings);
        }

        public void ApplyGameOptions() {
            ApplyGraphicsOptions();
            OnGameSettingsApplied?.Invoke();
        }

        private void ApplyGraphicsOptions() {
            QualitySettings.vSyncCount = Preferences.Instance.GetBool("graphics-vsync") ? 1 : 0;

            var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            urp.renderScale = Mathf.Clamp(Preferences.Instance.GetFloat("graphics-render-scale"), 0.5f, 2);

            // For some maddening reason soft shadows is not exposed but flipping this bool does work so here's some awful reflection. yay!
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

            // mip map quality via texture detail (0 = full, 1 = 1/4th, 2 = 1/16th)
            var textureDetail = Preferences.Instance.GetString("graphics-texture-detail");
            QualitySettings.globalTextureMipmapLimit = textureDetail switch {
                "high" => 0,
                "medium" => 1,
                "low" => 2,
                _ => 0
            };

            ssao.SetActive(Preferences.Instance.GetBool("graphics-ssao"));

            // reflections
            var shipPlayer = FdPlayer.FindLocalShipPlayer;
            if (shipPlayer != null) {
                var reflectionSetting = Preferences.Instance.GetString("graphics-reflections");
                shipPlayer.ReflectionProbe.gameObject.SetActive(reflectionSetting != "off");
                switch (reflectionSetting) {
                    case "ultra":
                        shipPlayer.ReflectionProbe.resolution = 512;
                        shipPlayer.ReflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                        break;
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
            IEnumerator StartVR() {
                // Ludicrous workaround for dumb shit a unity update broke
                // TODO: try not starting it twice in some magical future time
                yield return StartXR();
                StopXR();
                yield return new WaitForFixedUpdate();
                yield return StartXR();
                IsVREnabled = true;
                NotifyVRStatus();
            }

            StartCoroutine(StartVR());
        }

        public void DisableVRIfNeeded() {
            if (IsVREnabled) {
                StopXR();
                IsVREnabled = false;
                NotifyVRStatus();
            }
        }

        public void WaterTransitioned(bool submerged) {
            IsUnderWater = submerged;
            var player = FdPlayer.FindLocalShipPlayer;
            var velocity = Vector3.zero;
            if (player != null) velocity = player.Rigidbody.velocity;

            OnWaterTransition?.Invoke(IsUnderWater, velocity);
        }

        public void ResetHmdView(XROrigin xrOrigin, Transform targetTransform) {
            xrOrigin.MoveCameraToWorldLocation(targetTransform.position);
            xrOrigin.MatchOriginUpCameraForward(targetTransform.up, targetTransform.forward);

            var xrOriginTransform = xrOrigin.transform;
            _hmdPosition = xrOriginTransform.localPosition;
            _hmdRotation = xrOriginTransform.localRotation;

            Preferences.Instance.SetString("lastPlayedVersionInVR", Application.version);
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
                yield return FdPlayer.WaitForLoadingPlayer();
                var loadingPlayer = FdPlayer.LocalLoadingPlayer;

                loadingPlayer.ShowLoadingRoom();
                loadingPlayer.transform.position = levelData.startPosition.ToVector3();

                yield return _levelLoader.StartGame(levelData);

                // wait for all known currently loading players to have finished loading
                loadingPlayer.SetLoaded();
                var loadingRoom = FindObjectOfType<LoadingRoom>();
                loadingRoom.CenterLoadingText = "Waiting for all players to load ...";
                yield return WaitForAllPlayersLoaded();

                loadingPlayer.RequestTransitionToShipPlayer();

                SessionStatus = SessionStatus.InGame;

                // wait for local ship client object
                yield return FdPlayer.WaitForShipPlayer();
                var ship = FdPlayer.LocalShipPlayer;
                ship.User.ShipCameraRig.Reset();

                // Allow the rigid body to initialise before setting new parameters!
                yield return new WaitForEndOfFrame();

                // set up graphics settings (e.g. camera FoV)
                ApplyGameOptions();

#if !NO_PAID_ASSETS
                // gpu instancer VR initialisation (paid asset!)
                if (IsVREnabled) {
                    var cam = FindObjectOfType<XROrigin>(true).Camera;

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

                // if we have water in the world, toggle it on and off because some weird URP issue which doesn't handle the lighting properly on load
                // same for reflection probes because ?!?!?!?!
                // TODO: figure out why this jank happens, don't have time right now
                var water = FindObjectOfType<ModifierWater>();
                if (water != null) {
                    water.gameObject.SetActive(false);
                    var shouldShowReflectionProbes = ship.ReflectionProbe.gameObject.activeSelf;
                    ship.ReflectionProbe.gameObject.SetActive(false);

                    yield return new WaitForEndOfFrame();

                    water.gameObject.SetActive(true);
                    ship.ReflectionProbe.gameObject.SetActive(shouldShowReflectionProbes);
                }

                yield return _levelLoader.HideLoadingScreen();

                ship.ShipPhysics.FlightParameters = ShipParameters;

                // resume the game
                Time.timeScale = 1;

                SetFlatScreenCameraControllerActive(!IsVREnabled);

                // notify VR status (e.g. setting canvas world space, cameras, radial fog etc)
                NotifyVRStatus();

                FdConsole.Instance.LogMessage("Loaded level " + levelData.LevelHash());

                // if there's a track, initialise it
                var track = FindObjectOfType<Track>();
                if (track) {
                    var gameMode = levelData.gameType.GameMode;
                    GameModeHandler.InitialiseGameMode(ship, levelData, gameMode, ship.User.InGameUI, track);
                }
                else {
                    Debug.LogWarning("No track in the world! Cannot initialise game mode");
                }

                FadeFromBlack();
                yield return new WaitForSeconds(0.7f);

                // if there's a track in the game world, start it
                // if (track) yield return track.StartTrackWithCountdown();

                // store the starting position after any correction (e.g. move above terrain height)
                // TODO: What impact, if any, does this have on level hashes??
                var shipPosition = ship.AbsoluteWorldPosition;
                _levelLoader.LoadedLevelData.startPosition = SerializableVector3.FromVector3(shipPosition);

                // notify other players for e.g. targeting systems
                ship.CmdNotifyPlayerLoaded();

                GameModeHandler.Begin();
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
            if (SessionType == SessionType.Multiplayer && NetworkClient.activeHost)
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

            GameModeHandler.Quit();
            AudioMixer.Instance.Reset();

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
                foreach (var terrainTile in mapMagic.tiles.All()) terrainTile.StopGenerate();
                mapMagic.enabled = false;
            }

            yield return FdPlayer.WaitForShipPlayer();
            var ship = FdPlayer.FindLocalShipPlayer;
            if (ship != null) ship.User.DisableGameInput();

            IEnumerator LoadMenuScene() {
                // during load we pause scaled time to prevent *absolutely anything* from interacting incorrectly
                Time.timeScale = 1;

                FadeToBlack();
                MusicManager.Instance.StopMusic(true);
                yield return new WaitForSeconds(0.5f);
                yield return SceneManager.LoadSceneAsync("Main Menu");
                yield return new WaitForEndOfFrame();
                NotifyVRStatus();
                SetFlatScreenCameraControllerActive(false);
                yield return new WaitForEndOfFrame();
                ApplyGameOptions();
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
                Time.timeScale = 1;
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
                    var xrOrigin = FindObjectOfType<XROrigin>(true);

                    if (xrOrigin) {
                        // Whenever VR is enabled on a new version in any capacity (e.g. through option menu). perform auto-calibration.
                        if (Preferences.Instance.GetString("lastPlayedVersionInVR") != Application.version) {
                            Debug.Log("New version detected in VR session, performing auto-calibration");
                            ResetHmdView(xrOrigin, xrOrigin.transform.parent);
                        }

                        var xrTransform = xrOrigin.transform;

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
                if (replayObject != null && replayObject.Transform != null) Destroy(replayObject.Transform.gameObject);
                Destroy(shipGhost.gameObject);

                yield return new WaitForEndOfFrame();
                OnGhostRemoved?.Invoke();
            }

            StartCoroutine(DestroyGhost());
        }

        public void NotifyPlayerLoaded() {
            OnPlayerJoin?.Invoke();
        }

        private IEnumerator StartXR() {
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }

        private void StopXR() {
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }
    }
}