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
            
            // TODO: Level system for races - for now just load from json 
            var levelData = LevelData.FromJsonString("{\r\n  \"version\": 1,\r\n  \"name\": \"\",\r\n  \"location\": 1,\r\n  \"terrainSeed\": \"\",\r\n  \"startPosition\": {\r\n    \"x\": 0,\r\n    \"y\": 0,\r\n    \"z\": 0\r\n  },\r\n  \"startRotation\": {\r\n    \"x\": 0,\r\n    \"y\": 0,\r\n    \"z\": 0\r\n  },\r\n  \"raceType\": 1,\r\n  \"checkpoints\": [\r\n    {\r\n      \"type\": 0,\r\n      \"position\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 2226.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 687.0,\r\n        \"y\": 0.0,\r\n        \"z\": 4382.67\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 16.568428,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 2448.0,\r\n        \"y\": 0.0,\r\n        \"z\": 6045.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 90.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 4878.0,\r\n        \"y\": 1449.0,\r\n        \"z\": 405.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 4878.0,\r\n        \"y\": 1449.0,\r\n        \"z\": -1356.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 2481.0,\r\n        \"y\": 2601.0,\r\n        \"z\": -3402.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 90.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 2,\r\n      \"position\": {\r\n        \"x\": 0.0,\r\n        \"y\": 2628.0,\r\n        \"z\": 384.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    }\r\n  ]\r\n}");
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
