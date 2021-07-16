using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Player;
using Den.Tools;
using JetBrains.Annotations;
using MapMagic.Core;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

namespace Core {

    public enum SessionType {
        Singleplayer,
        Multiplayer
    }
    public class Game : MonoBehaviour {

        public static Game Instance;

        public delegate void RestartLevelAction();
        public delegate void GraphicsSettingsApplyAction();
        public delegate void VRToggledAction(bool enabled);
        public static event RestartLevelAction OnRestart;
        public static event GraphicsSettingsApplyAction OnGraphicsSettingsApplied;
        public static event VRToggledAction OnVRStatus;

        [SerializeField] private InputActionAsset playerBindings;
        [SerializeField] private ScriptableRendererFeature ssao;
        private ShipParameters _shipParameters;
        private Vector3 _hmdPosition;
        private Quaternion _hmdRotation;
        private LevelLoader _levelLoader;
        private SessionType _sessionType = SessionType.Singleplayer;
        private bool _isVREnabled = false;

        // TODO: This must be done via network manager to transition state
        [SerializeField] private ShipPlayer shipPlayerPrefab;
        
        // The level data most recently used to load a map
        public LevelData LoadedLevelData => _levelLoader.LoadedLevelData;
        // The level data hydrated with the current player position and track layout
        public LevelData LevelDataAtCurrentPosition => _levelLoader.LevelDataAtCurrentPosition;
        
        public SessionType SessionType => _sessionType;
        public bool IsVREnabled => _isVREnabled;

        public ShipParameters ShipParameters {
            get => _shipParameters == null
                ? FindObjectOfType<ShipPlayer>()?.Parameters ?? ShipPlayer.ShipParameterDefaults
                : _shipParameters;
            set {
                _shipParameters = value;
                var ship = FindObjectOfType<ShipPlayer>();
                if (ship) ship.Parameters = _shipParameters;
            }
        }

        public bool IsTerrainMap =>
            _levelLoader.LoadedLevelData.location == Location.TerrainV1 ||
            _levelLoader.LoadedLevelData.location == Location.TerrainV2;

        public string Seed => _levelLoader.LoadedLevelData.terrainSeed;

        [SerializeField] private Animator crossfade;

        // show certain things if first time hitting the menu
        private bool _menuFirstRun = true;
        public bool menuFirstRun => _menuFirstRun;

        void Awake() {
            // singleton shenanigans
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
                return;
            }
        }

        public void Start() {
            // must be a level loader in the scene
            _levelLoader = FindObjectOfType<LevelLoader>();

            // if there's a user object when the game starts, enable input (usually in the editor!)
            FindObjectOfType<User>()?.EnableGameInput();
            LoadBindings();
            ApplyGraphicsOptions();

            // We use a custom canvas cursor to work in VR and pancake
            Cursor.visible = false;

            // check for command line args
            var args = System.Environment.GetCommandLineArgs();
            if (args.ToList().Contains("-vr") || args.ToList().Contains("-VR")) {
                EnableVR();
            }
        }

        private void OnDestroy() {
            DisableVRIfNeeded();
        }

        private void OnApplicationQuit() {
            DisableVRIfNeeded();
        }

        public void LoadBindings() {
            var bindings = Preferences.Instance.GetString("inputBindings");
            if (!string.IsNullOrEmpty(bindings)) {
                playerBindings.LoadBindingOverridesFromJson(bindings);
            }
        }

        public void ApplyGraphicsOptions() {
            if (OnGraphicsSettingsApplied != null) {
                OnGraphicsSettingsApplied();
            }

            var urp = (UniversalRenderPipelineAsset) GraphicsSettings.currentRenderPipeline;
            urp.renderScale = Preferences.Instance.GetFloat("graphics-render-scale");
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
                yield return UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.InitializeLoader();
                UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.StartSubsystems();
                _isVREnabled = true;
                NotifyVRStatus();
            }

            StartCoroutine(StartXR());
        }

        public void DisableVRIfNeeded() {
            if (IsVREnabled) {
                UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.StopSubsystems();
                UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.DeinitializeLoader();
                _isVREnabled = false;
                NotifyVRStatus();
            }
        }

        public void ResetHMDView(XRRig xrRig, Transform targetTransform) {
            var before = xrRig.transform.position;
            xrRig.MoveCameraToWorldLocation(targetTransform.position);
            xrRig.MatchRigUpCameraForward(targetTransform.up, targetTransform.forward);
            _hmdRotation = xrRig.transform.rotation;
            _hmdPosition += xrRig.transform.position - before;
        }

        public void StartGame(SessionType sessionType, LevelData levelData, bool dynamicPlacementStart = false) {

            /* Split this into single and multiplayer - logic should be mostly the same but we don't
                transition from a lobby and we need to set the queryable SessionType for other logic in-game
                (e.g. no actual pause on pause menu, quick to menu being quit to lobby etc)
            */
            _sessionType = sessionType;

            // find the local lobby player and transition it, if it exists
            var lobbyPlayer = LobbyPlayer.FindLocal;
            if (_sessionType == SessionType.Multiplayer && lobbyPlayer) {
                FdNetworkManager.Instance.TransitionToLoadingPlayer(lobbyPlayer);
            }
            
            LockCursor();

            IEnumerator LoadGame() {
                yield return _levelLoader.SwitchToLoadingScreen();
                // TODO: network player potential transition from lobby player prefab to loading player prefab
                yield return _levelLoader.StartGame(levelData);

                yield return FdNetworkManager.Instance.WaitForAllPlayersLoaded();

                var loadingPlayer = LoadingPlayer.FindLocal;
                var ship = FdNetworkManager.Instance.TransitionToShipPlayer(loadingPlayer);
                yield return new WaitForEndOfFrame();
                
                if (ship) {
                    // debug flight params
                    ship.Parameters = ShipParameters;

                    ship.transform.position = new Vector3(
                        LoadedLevelData.startPosition.x,
                        LoadedLevelData.startPosition.y,
                        LoadedLevelData.startPosition.z
                    );
                    ship.transform.rotation = Quaternion.Euler(
                        LoadedLevelData.startRotation.x,
                        LoadedLevelData.startRotation.y,
                        LoadedLevelData.startRotation.z
                    );

                    // terrain loaded, if we need to dynamically place the ship let's do that now
                    if (dynamicPlacementStart) {
                        // TODO: make this iterate over the corners of the ship:
                        // move the player up high and perform 5 raycasts - one from each corner of the ship and one from the centre.
                        // move the player to the closest one, height-wise.
                        // Additionally, move the ship around in a spiral and perform this operation a number of times.
                        // Move the ship to the lowest position.

                        var shipTransform = ship.transform;

                        // move ship above terrain max
                        shipTransform.position = new Vector3(
                            shipTransform.position.x,
                            10000,
                            shipTransform.position.z
                        );

                        // cast down to get terrain height at this position
                        if (Physics.Raycast(ship.transform.position, Vector3.down, out var hit, 10000)) {
                            shipTransform.position = hit.point;

                            // move ship 25 meters up to compensate for rocks and other crap
                            shipTransform.Translate(0, 25, 0);

                            // store new position in game level data for restarts
                            LoadedLevelData.startPosition.x = shipTransform.position.x;
                            LoadedLevelData.startPosition.y = shipTransform.position.y;
                            LoadedLevelData.startPosition.z = shipTransform.position.z;
                        }
                    }
                }


                // set up graphics settings (e.g. camera FoV) + VR status (cameras, radial fog etc)
                ApplyGraphicsOptions();
                NotifyVRStatus();

                // resume the game
                Time.timeScale = 1;
                FadeFromBlack();
                yield return new WaitForSeconds(0.7f);

                // if there's a track in the game world, start it
                var track = FindObjectOfType<Track>();
                if (track) {
                    yield return track.StartTrackWithCountdown();
                }

                // enable user input
                var user = FindObjectOfType<User>();
                if (user != null) {
                    user.EnableGameInput();
                }
            }

            StartCoroutine(LoadGame());
        }

        public void RestartLevel() {
            StartCoroutine(_levelLoader.RestartLevel(() => {
                if (OnRestart != null) {
                    OnRestart();
                }
            }));
        }

        public void QuitToMenu() {
            FdNetworkManager.Instance.CloseConnection();
            
            _menuFirstRun = false;
            var mapMagic = FindObjectOfType<MapMagicObject>();
            if (mapMagic) {
                mapMagic.StopGenerate();
            }

            var user = FindObjectOfType<User>();
            user.DisableGameInput();

            IEnumerator LoadMenu() {
                FadeToBlack();
                yield return new WaitForSeconds(0.5f);
                SceneManager.LoadScene("Main Menu");
                _levelLoader.ResetLoadedLevelData();
                ApplyGraphicsOptions();
                NotifyVRStatus();
                FreeCursor();
            }

            StartCoroutine(LoadMenu());
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
            Cursor.lockState = CursorLockMode.Confined;
        }

        public void FadeToBlack() {
            crossfade.SetTrigger("FadeToBlack");
        }

        public void FadeFromBlack() {
            crossfade.SetTrigger("FadeFromBlack");
        }

        private void NotifyVRStatus() {
            if (OnVRStatus != null) {
                OnVRStatus(IsVREnabled);
            }

            // if user has previously applied a HMD position, reapply
            if (IsVREnabled) {
                var xrRig = FindObjectOfType<XRRig>();
                if (xrRig) {
                    xrRig.transform.rotation = _hmdRotation;
                    xrRig.transform.localPosition = xrRig.transform.localPosition + _hmdPosition;
                }
            }
        }
    }
}