using System.Collections;
using System.Collections.Generic;
using Audio;
using Menus.Options;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Menus {
    public class MainMenu : MonoBehaviour {

        [SerializeField]
        private TopMenu topMenu;
        
        [SerializeField]
        private OptionsMenu optionsMenu;

        [SerializeField] private Animator crossfade;
        [SerializeField] private GameObject shipMesh;
        [SerializeField] private GameObject alphaMessage;

        private List<AsyncOperation> scenesLoading = new List<AsyncOperation>();
        
        // Start is called before the first frame update
        void Awake() {
            StartCoroutine(ShowAlphaMessage());
        }

        // Update is called once per frame
        void FixedUpdate() {
            // move along at a fixed rate to animate the stars
            // dirty hack job but who cares it's a menu screen
            transform.Translate(0.1f, 0, 0.5f);

            // gently rock the ship mesh back and forth
            var rotationAmount = (0.25f - Mathf.PingPong(Time.time / 20, 0.5f)) / 5;
            shipMesh.transform.Rotate(Vector3.forward, rotationAmount);
        }

        public void Race() {
            scenesLoading.Add(SceneManager.LoadSceneAsync("MapTest", LoadSceneMode.Additive));
            StartGame();
        }

        public void Freeplay() {
            scenesLoading.Add(SceneManager.LoadSceneAsync("TerrainTest", LoadSceneMode.Additive));
            StartGame();
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
            Application.Quit();
            AudioManager.Instance.Play("ui-cancel");
        }

        private void StartGame() {
            AudioManager.Instance.Play("ui-confirm");
            scenesLoading.Add(SceneManager.LoadSceneAsync("Player", LoadSceneMode.Additive));
            scenesLoading.ForEach(scene => scene.allowSceneActivation = false);
            topMenu.Hide();
            StartCoroutine(LoadScenes());
        }
        
        IEnumerator ShowAlphaMessage() {
            // if it's disabled in the editor don't show this fade animation
            if (alphaMessage.activeSelf) {
                shipMesh.SetActive(false);
                yield return new WaitForSeconds(6);
                crossfade.SetTrigger("FadeToBlack");
                yield return new WaitForSeconds(1);
                alphaMessage.SetActive(false);
                crossfade.SetTrigger("FadeFromBlack");
                shipMesh.SetActive(true);
            }
        }

        IEnumerator LoadScenes() {
            
            // disable event system and audio listener (camera) here - may only have one of each active and cannot unload a scene until others are loaded
            var eventSystems = FindObjectsOfType<EventSystem>();
            var audioListeners = FindObjectsOfType<AudioListener>();
            foreach (var eventSystem in eventSystems) {
                eventSystem.enabled = false;
            }
            foreach (var audioListener in audioListeners) {
                audioListener.enabled = false;
            }
            
            crossfade.SetTrigger("FadeToBlack");
            yield return new WaitForSeconds(1);
            Scene currentScene = SceneManager.GetActiveScene();
            
            // float progress = 0;
            for (int i = 0; i < scenesLoading.Count; ++i) {
                while (scenesLoading[i].progress < 0.9f) { // this is literally what the unity docs recommend
                    yield return null;
                    
                    // TODO: loading bar (eventually - not really necessary yet)
                    // progress += scenesLoading[i].progress;
                    // totalProgress = progress / scenesLoading.Count;
                    // Debug.Log(i + " " + scenesLoading[i].progress);
                    yield return null;
                }
            }
            
            // all scenes have loaded as far as they can without activation, allow them to activate
            for (int i = 0; i < scenesLoading.Count; ++i) {
                scenesLoading[i].allowSceneActivation = true;
                while (!scenesLoading[i].isDone) {
                    yield return null;
                }
            }

            // unload current scene
            AsyncOperation unload = SceneManager.UnloadSceneAsync(currentScene);

            while (!unload.isDone) {
                yield return null;
            }
        }
    }
}
