using System.Collections;
using System.Linq;
using Audio;
using Core;
using Core.MapData;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

namespace Menus.Main_Menu {
    public class MainMenu : MonoBehaviour {
        // Animating the ship
        [SerializeField] private GameObject shipMesh;

        // VR handling
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera flatScreenCamera;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private XRRig xrRig;

        [SerializeField] private VRCalibrationDialog vrCalibrationDialog;
        [SerializeField] private NewPlayerWelcomeDialog newPlayerWelcomeDialog;
        [SerializeField] private TopMenu topMenu;
        [SerializeField] private ProfileMenu profileMenu;
        [SerializeField] private DisconnectionDialog disconnectionDialog;

        private static bool FirstRun => Game.Instance.MenuFirstRun;

        private void Start() {
            topMenu.Hide();

            var lastPlayedVersion = Preferences.Instance.GetString("lastPlayedVersion");
            topMenu.SetPatchNotesUpdated(lastPlayedVersion != Application.version);
        }

        private void FixedUpdate() {
            // gently rock the ship mesh back and forth
            var rotationAmount = (0.25f - Mathf.PingPong(Time.time / 5, 0.5f)) / 5;
            shipMesh.transform.Rotate(Vector3.forward, rotationAmount);
        }

        private void OnEnable() {
            SceneManager.sceneLoaded += OnEnvironmentLoadComplete;
            Game.OnVRStatus += OnVRStatus;
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
            InputSystem.onDeviceChange += OnDeviceChange;
            StartCoroutine(MenuLoad());
        }

        private void OnDisable() {
            SceneManager.sceneLoaded -= OnEnvironmentLoadComplete;
            Game.OnVRStatus -= OnVRStatus;
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        public void ShowDisconnectedDialog(string reason) {
            topMenu.gameObject.SetActive(false);
            disconnectionDialog.Open(topMenu);
            disconnectionDialog.Reason = reason;
        }

        // TODO: show time trial with previous level selected

        public void ShowLobby() {
            var lobby = FindObjectOfType<LobbyMenu>(true);
            if (lobby) {
                topMenu.Hide();
                lobby.Open(topMenu);
            }
            else {
                Debug.LogWarning("Failed to find lobby!");
            }
        }

        [UsedImplicitly]
        public void OnResetHMDView(InputValue inputValue) {
            if (xrRig) Game.Instance.ResetHmdView(xrRig, xrRig.transform.parent);
        }

        private void OnVRStatus(bool isVREnabled) {
            // if VR is enabled, we need to swap our active cameras and make UI panels operate in world space
            if (isVREnabled) {
                canvas.renderMode = RenderMode.WorldSpace;
                var canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect) {
                    canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1920);
                    canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1080);
                }

                canvas.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
                flatScreenCamera.enabled = false;
                uiCamera.enabled = false;
                xrRig.gameObject.SetActive(true);
            }
            else {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                flatScreenCamera.enabled = true;
                uiCamera.enabled = true;
                xrRig.gameObject.SetActive(false);
            }
        }

        private void OnGameSettingsApplied() {
            var playerInput = GetComponent<PlayerInput>();
            playerInput.currentActionMap = playerInput.actions
                .FindActionMap(Preferences.Instance.GetString("controlSchemeType") == "arcade" ? "GlobalArcade" : "Global");
        }

        private IEnumerator MenuLoad() {
            // input jank - disable and re-enable
            var playerInput = GetComponent<PlayerInput>();
            playerInput.user.ActivateControlScheme("Everything");
            playerInput.enabled = false;

            // load engine if not already 
            if (!FindObjectOfType<Engine>()) yield return SceneManager.LoadSceneAsync("Engine", LoadSceneMode.Additive);

            var sceneEnvironment = Environment.PlanetOrbitBottom;

            if (!FirstRun) {
                // If it's not the first run, switch up the title screen :D
                var environmentIndex = Random.Range(0, 6);
                switch (environmentIndex) {
                    case 0:
                        sceneEnvironment = Environment.PlanetOrbitBottom;
                        break;
                    case 1:
                        sceneEnvironment = Environment.PlanetOrbitTop;
                        break;
                    case 2:
                        sceneEnvironment = Environment.SunriseClear;
                        break;
                    case 3:
                        sceneEnvironment = Environment.NoonClear;
                        break;
                    case 4:
                        sceneEnvironment = Environment.SunsetClear;
                        break;
                }
            }

            yield return SceneManager.LoadSceneAsync(sceneEnvironment.SceneToLoad, LoadSceneMode.Additive);
            yield return new WaitForEndOfFrame();

            MusicManager.Instance.PlayMusic(MusicTrack.MainMenu, FirstRun, false, false);
            Game.Instance.SetFlatScreenCameraControllerActive(false);

            FloatingOrigin.Instance.FocalTransform = transform;

            // enable input and forcefully pair ALL devices (I have no idea why we have to do this)
            playerInput.enabled = true;
            foreach (var inputDevice in InputSystem.devices)
                if (!playerInput.devices.Contains(inputDevice))
                    InputUser.PerformPairingWithDevice(inputDevice, playerInput.user);

            yield return new WaitForSecondsRealtime(0.1f);
            Game.Instance.FadeFromBlack();
            ShowStartingPanel();
        }

        private void ShowStartingPanel() {
            topMenu.Hide();

            var lastPlayedVersion = Preferences.Instance.GetString("lastPlayedVersion");
            var showVrCalibrationDialog = Preferences.Instance.GetVector3("hmdPosition").Equals(Vector3.zero);
            var showWelcomeDialog = lastPlayedVersion == "none";

            // TODO: go back to last known panel on game mode quit? (e.g. time trial etc)
            // (handling the offline server stuff will probably be pain!)

            // startup stuff
            if (Game.Instance.IsVREnabled && showVrCalibrationDialog) {
                vrCalibrationDialog.Open(null);
                vrCalibrationDialog.showWelcomeDialogNext = showWelcomeDialog;
            }
            else if (showWelcomeDialog) {
                newPlayerWelcomeDialog.Open(profileMenu);
            }
            else {
                topMenu.gameObject.SetActive(true);
            }

            Preferences.Instance.SetString("lastPlayedVersion", Application.version);
            Preferences.Instance.Save();
        }

        private void OnEnvironmentLoadComplete(Scene scene, LoadSceneMode mode) {
            SceneManager.SetActiveScene(scene);
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change) {
            var playerInput = GetComponent<PlayerInput>();
            if (change == InputDeviceChange.Added) InputUser.PerformPairingWithDevice(device, playerInput.user);
            if (change == InputDeviceChange.Removed) playerInput.user.UnpairDevice(device);
        }
    }
}