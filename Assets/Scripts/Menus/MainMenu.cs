using System.Collections;
using System.Collections.Generic;
using Audio;
using Engine;
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
        
        [SerializeField]
        private FreeRoamMenu freeRoamMenu;
        [SerializeField]
        private LoadCustomMenu loadCustomMenu;

        [SerializeField] private GameObject shipMesh;
        [SerializeField] private GameObject alphaMessage;
        
        // Start is called before the first frame update
        void OnEnable() {
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
            AudioManager.Instance.Play("ui-confirm");
            
            var levelData = new LevelData();
            levelData.location = Location.TestSpaceStation;
            levelData.raceType = RaceType.Sprint;
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

        IEnumerator ShowAlphaMessage() {
            if (Game.Instance?.menuFirstRun ?? true) {
                // if it's disabled in the editor don't show this fade animation
                if (alphaMessage.activeSelf) {
                    topMenu.Hide();
                    shipMesh.SetActive(false);
                    yield return new WaitForSeconds(8);
                    Game.Instance.FadeToBlack();
                    yield return new WaitForSeconds(1);
                    alphaMessage.SetActive(false);
                    Game.Instance.FadeFromBlack();
                    topMenu.Show();
                    shipMesh.SetActive(true);
                }
            }
            else {
                alphaMessage.SetActive(false);
            }
        }
    }
}
