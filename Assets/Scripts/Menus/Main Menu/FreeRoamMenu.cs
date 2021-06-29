using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Core;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Environment = Core.Environment;

namespace Menus.Main_Menu {
    public class FreeRoamMenu : MonoBehaviour {
        [SerializeField] private SinglePlayerMenu singlePlayerMenu;
        [SerializeField] private InputField seedInput;
        [SerializeField] private Button defaultActiveButton;

        [CanBeNull] private LevelData _levelData;
        [SerializeField] private Dropdown conditionsSelector;

        private Animator _animator;

        private void Awake() {
            this._animator = this.GetComponent<Animator>();
        }

        public void Hide() {
            this.gameObject.SetActive(false);
        }

        public void Show() {
            this.gameObject.SetActive(true);
            defaultActiveButton.Select();
            this._animator.SetBool("Open", true);
        }

        public void ClosePanel() {
            AudioManager.Instance.Play("ui-cancel");
            singlePlayerMenu.Show();
            Hide();
        }

        private void OnEnable() {
            seedInput.text = Guid.NewGuid().ToString();
            _levelData = null;
        }

        public void OnSeedInputFieldChanged(string seed) {
            if (seedInput.text.Length == 0) {
                seedInput.text = Guid.NewGuid().ToString();
            }
        }

        public void StartFreeRoam() {
            var levelData = _levelData != null ? _levelData : new LevelData();
            levelData.location = Preferences.Instance.GetBool("enableExperimentalTerrain")
                ? Location.TerrainV2
                : Location.TerrainV1;
            levelData.raceType = RaceType.None;
            levelData.terrainSeed = seedInput.text;

            switch (conditionsSelector.value) {
                case 0:
                    levelData.environment = Environment.SunriseClear;
                    break;
                case 1:
                    levelData.environment = Environment.NoonClear;
                    break;
                case 2:
                    levelData.environment = Environment.NoonCloudy;
                    break;
                case 3:
                    levelData.environment = Environment.NoonStormy;
                    break;
                case 4:
                    levelData.environment = Environment.SunsetClear;
                    break;
                case 5:
                    levelData.environment = Environment.SunsetCloudy;
                    break;
                case 6:
                    levelData.environment = Environment.NightClear;
                    break;
                case 7:
                    levelData.environment = Environment.NightCloudy;
                    break;
            }

            Game.Instance.StartGame(levelData, true);
        }
    }
}