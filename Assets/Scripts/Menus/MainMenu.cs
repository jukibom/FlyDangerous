using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Engine;
using Menus.Options;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

namespace Menus {
    public class MainMenu : MonoBehaviour {

        [SerializeField] private Canvas canvas;
        [SerializeField] private TopMenu topMenu;
        [SerializeField] private OptionsMenu optionsMenu;
        [SerializeField] private FreeRoamMenu freeRoamMenu;
        [SerializeField] private LoadCustomMenu loadCustomMenu;
        [SerializeField] private GameObject shipMesh;

        [SerializeField] private Camera flatScreenCamera;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private XRRig xrRig;

        public static bool FirstRun => Game.Instance?.menuFirstRun ?? true;

        void OnEnable() {
            SceneManager.sceneLoaded += OnEnvironmentLoadComplete;
            Game.OnVRStatus += OnVRStatus;
            StartCoroutine(MenuLoad());
        }

        private void OnDisable() {
            SceneManager.sceneLoaded -= OnEnvironmentLoadComplete;
            Game.OnVRStatus -= OnVRStatus;
        }

        private void FixedUpdate() {
            // move along at a fixed rate to animate the stars
            // dirty hack job but who cares it's a menu screen
            transform.Translate(0.1f, 0, 0.5f);

            // gently rock the ship mesh back and forth
            var rotationAmount = (0.25f - Mathf.PingPong(Time.time / 20, 0.5f)) / 5;
            shipMesh.transform.Rotate(Vector3.forward, rotationAmount);
        }

        public void OnResetHMDView(InputValue inputValue) {
            if (xrRig) {
                Game.Instance.ResetHMDView(xrRig, flatScreenCamera.transform);
            }
        }
        
        public void OnVRStatus(bool isVREnabled) {
            // if VR is enabled, we need to swap our active cameras and make UI panels operate in world space
            if (isVREnabled) {
                canvas.renderMode = RenderMode.WorldSpace;
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

        public void Race() {
            AudioManager.Instance.Play("ui-confirm");
            
            // TODO: Level system for races - for now just load from json 
            var levelData = LevelData.FromJsonString("{\r\n  \"version\": 1,\r\n  \"name\": \"\",\r\n  \"location\": 1,\r\n  \"environment\": 1,\r\n   \"terrainSeed\": \"\",\r\n  \"startPosition\": {\r\n    \"x\": 0,\r\n    \"y\": 0,\r\n    \"z\": 0\r\n  },\r\n  \"startRotation\": {\r\n    \"x\": 0,\r\n    \"y\": 0,\r\n    \"z\": 0\r\n  },\r\n  \"raceType\": 1,\r\n  \"checkpoints\": [\r\n    {\r\n      \"type\": 0,\r\n      \"position\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 2226.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 687.0,\r\n        \"y\": 0.0,\r\n        \"z\": 4382.67\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 16.568428,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 2448.0,\r\n        \"y\": 0.0,\r\n        \"z\": 6045.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 90.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 4878.0,\r\n        \"y\": 1449.0,\r\n        \"z\": 405.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 4878.0,\r\n        \"y\": 1449.0,\r\n        \"z\": -1356.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 2481.0,\r\n        \"y\": 2601.0,\r\n        \"z\": -3402.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 90.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 2,\r\n      \"position\": {\r\n        \"x\": 0.0,\r\n        \"y\": 2628.0,\r\n        \"z\": 384.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    }\r\n  ]\r\n}");
            Game.Instance.StartGame(levelData);
            
            topMenu.Hide();
        }

        public void OpenFreeRoamPanel() {
            AudioManager.Instance.Play("ui-dialog-open");
            freeRoamMenu.Show();
            topMenu.Hide();
        }
        
        public void CloseFreeRoamPanel() {
            freeRoamMenu.Hide();
            topMenu.Show();
        }

        public void OpenLoadCustomPanel() {
            AudioManager.Instance.Play("ui-dialog-open");
            loadCustomMenu.Show();
            topMenu.Hide();
        }

        public void CloseLoadCustomPanel() {
            loadCustomMenu.Hide();
            topMenu.Show();
        }

        public void OpenOptionsPanel() {
            AudioManager.Instance.Play("ui-dialog-open");
            topMenu.Hide();
            optionsMenu.Show();
        }

        public void CloseOptionsPanel() {
            optionsMenu.Hide();
            topMenu.Show();
        }
        
        public void OpenDiscordLink() {
            AudioManager.Instance.Play("ui-dialog-open");
            Application.OpenURL("https://discord.gg/4daSEUKZ6A");
        }

        public void Quit() {
            Game.Instance.QuitGame();
            AudioManager.Instance.Play("ui-cancel");
        }

        IEnumerator MenuLoad() {
            string sceneEnvironment = "Planet_Orbit_Bottom";
            
            if (!FirstRun) {
                // If it's not the first run, switch up the title screen :D
                int environmentIndex = Random.Range(0, 5);
                switch (environmentIndex) {
                    case 0: sceneEnvironment = "Planet_Orbit_Bottom"; break;
                    case 1: sceneEnvironment = "Sunrise_Clear"; break;
                    case 2: sceneEnvironment = "Noon_Clear"; break;
                    case 3: sceneEnvironment = "Noon_Cloudy"; break;
                    case 4: sceneEnvironment = "Sunset_Clear"; break;
                }
            }
            yield return SceneManager.LoadSceneAsync(sceneEnvironment, LoadSceneMode.Additive);
            Game.Instance.FadeFromBlack();
        }

        private void OnEnvironmentLoadComplete(Scene scene, LoadSceneMode mode) {
            SceneManager.SetActiveScene(scene);
        }
    }
}
