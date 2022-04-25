using System.Collections;
using System.Linq;
using Core;
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

        [SerializeField] private NewPlayerWelcomeDialog newPlayerWelcomeDialog;
        [SerializeField] private TopMenu topMenu;
        [SerializeField] private ProfileMenu profileMenu;
        [SerializeField] private DisconnectionDialog disconnectionDialog;

        [SerializeField] private bool shouldMove;
        [SerializeField] private float shipSpeed = 6f;

        private static bool FirstRun => Game.Instance.MenuFirstRun;

        private void Start() {
            // if(SteamManager.Initialized) {
            //     string name = SteamFriends.GetPersonaName();
            //     Debug.Log($"Your Steam name is {name}.");
            // }

            var lastPlayedVersion = Preferences.Instance.GetString("lastPlayedVersion");
            topMenu.SetPatchNotesUpdated(lastPlayedVersion != Application.version);
        }

        private void FixedUpdate() {
            if (shouldMove) {
                // move along at a fixed rate to animate the stars
                // dirty hack job but who cares it's a menu screen
                transform.Translate(0.1f, 0, shipSpeed);

                // gently rock the ship mesh back and forth
                var rotationAmount = (0.25f - Mathf.PingPong(Time.time / 20, 0.5f)) / 5;
                shipMesh.transform.Rotate(Vector3.forward, rotationAmount);
            }
        }

        private void OnEnable() {
            SceneManager.sceneLoaded += OnEnvironmentLoadComplete;
            Game.OnVRStatus += OnVRStatus;
            StartCoroutine(MenuLoad());
        }

        private void OnDisable() {
            SceneManager.sceneLoaded -= OnEnvironmentLoadComplete;
            Game.OnVRStatus -= OnVRStatus;
        }

        public void ShowDisconnectedDialog(string reason) {
            topMenu.gameObject.SetActive(false);
            disconnectionDialog.Open(topMenu);
            disconnectionDialog.Reason = reason;
        }

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

        public void OnVRStatus(bool isVREnabled) {
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

        private IEnumerator MenuLoad() {
            // input jank - disable and re-enable
            var playerInput = GetComponent<PlayerInput>();
            playerInput.user.ActivateControlScheme("Everything");
            playerInput.enabled = false;

            // load engine if not already 
            if (!FindObjectOfType<Engine>()) yield return SceneManager.LoadSceneAsync("Engine", LoadSceneMode.Additive);

            var sceneEnvironment = "Planet_Orbit_Bottom";

            if (!FirstRun) {
                // If it's not the first run, switch up the title screen :D
                var environmentIndex = Random.Range(0, 5);
                switch (environmentIndex) {
                    case 0:
                        sceneEnvironment = "Planet_Orbit_Bottom";
                        break;
                    case 1:
                        sceneEnvironment = "Sunrise_Clear";
                        break;
                    case 2:
                        sceneEnvironment = "Noon_Clear";
                        break;
                    case 3:
                        sceneEnvironment = "Noon_Cloudy";
                        break;
                    case 4:
                        sceneEnvironment = "Sunset_Clear";
                        break;
                }
            }

            yield return SceneManager.LoadSceneAsync(sceneEnvironment, LoadSceneMode.Additive);
            yield return new WaitForEndOfFrame();
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

            // TODO: go back to last known panel? (e.g. time trial etc)
            // (handling the offline server stuff will probably be pain!)
            var lastPlayedVersion = Preferences.Instance.GetString("lastPlayedVersion");
            if (lastPlayedVersion == "none") newPlayerWelcomeDialog.Open(profileMenu);
            else topMenu.gameObject.SetActive(true);

            Preferences.Instance.SetString("lastPlayedVersion", Application.version);
            Preferences.Instance.Save();
        }

        private void OnEnvironmentLoadComplete(Scene scene, LoadSceneMode mode) {
            SceneManager.SetActiveScene(scene);
        }
    }
}