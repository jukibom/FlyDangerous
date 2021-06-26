using Audio;
using Engine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class SinglePlayerMenu : MonoBehaviour {

        [SerializeField] private Button defaultActiveButton;
        [SerializeField] private TopMenu topMenu;
        [SerializeField] private FreeRoamMenu freeRoamMenu;
        [SerializeField] private LoadCustomMenu loadCustomMenu;
        private Animator _animator;
        
        private void Awake() {
            _animator = GetComponent<Animator>();
        }
        
        public void Show() {
            gameObject.SetActive(true);
            _animator.SetBool("Open", true);
            defaultActiveButton.Select();
        }
        
        public void Hide() {
            gameObject.SetActive(false);
        }

        public void ClosePanel() {
            AudioManager.Instance.Play("ui-cancel");
            topMenu.Show();
            Hide();
        }

        public void OpenCampaignPanel() {
            Debug.Log("Nope not yet!");
        }

        public void OpenTimeTrialPanel() {
            // TODO: An actual map select! Jesus!
            AudioManager.Instance.Play("ui-confirm");
            
            // TODO: Level system for races - for now just load from json 
            var levelData = LevelData.FromJsonString("{\r\n  \"version\": 1,\r\n  \"name\": \"\",\r\n  \"location\": 1,\r\n  \"environment\": 1,\r\n   \"terrainSeed\": \"\",\r\n  \"startPosition\": {\r\n    \"x\": 0,\r\n    \"y\": 0,\r\n    \"z\": 0\r\n  },\r\n  \"startRotation\": {\r\n    \"x\": 0,\r\n    \"y\": 0,\r\n    \"z\": 0\r\n  },\r\n  \"raceType\": 1,\r\n  \"checkpoints\": [\r\n    {\r\n      \"type\": 0,\r\n      \"position\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 2226.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 687.0,\r\n        \"y\": 0.0,\r\n        \"z\": 4382.67\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 16.568428,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 2448.0,\r\n        \"y\": 0.0,\r\n        \"z\": 6045.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 90.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 4878.0,\r\n        \"y\": 1449.0,\r\n        \"z\": 405.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 4878.0,\r\n        \"y\": 1449.0,\r\n        \"z\": -1356.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 1,\r\n      \"position\": {\r\n        \"x\": 2481.0,\r\n        \"y\": 2601.0,\r\n        \"z\": -3402.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 90.0,\r\n        \"z\": 0.0\r\n      }\r\n    },\r\n    {\r\n      \"type\": 2,\r\n      \"position\": {\r\n        \"x\": 0.0,\r\n        \"y\": 2628.0,\r\n        \"z\": 384.0\r\n      },\r\n      \"rotation\": {\r\n        \"x\": 0.0,\r\n        \"y\": 0.0,\r\n        \"z\": 0.0\r\n      }\r\n    }\r\n  ]\r\n}");
            Game.Instance.StartGame(levelData);
            
            Hide();
        }

        public void OpenFreeRoamPanel() {
            AudioManager.Instance.Play("ui-dialog-open");
            freeRoamMenu.Show();
            Hide();
        }

        public void OpenLoadCustomPanel() {
            AudioManager.Instance.Play("ui-dialog-open");
            loadCustomMenu.Show();
            Hide();
        }
    }
}