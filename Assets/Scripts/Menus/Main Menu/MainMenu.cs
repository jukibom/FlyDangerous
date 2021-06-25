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

namespace Menus.Main_Menu {
    public class MainMenu : MonoBehaviour {

        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject shipMesh;

        [SerializeField] private Camera flatScreenCamera;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private XRRig xrRig;

        public static bool FirstRun => Game.Instance?.menuFirstRun ?? true;

        void OnEnable() {
            SceneManager.sceneLoaded += OnEnvironmentLoadComplete;
            Game.OnVRStatus += OnVRStatus;
            FloatingOrigin.Instance.focalTransform = transform;
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
