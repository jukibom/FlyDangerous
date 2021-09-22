using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Core;
using Core.MapData;
using JetBrains.Annotations;
using Misc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Environment = Core.MapData.Environment;


namespace Menus.Main_Menu {
    public class FreeRoamMenu : MonoBehaviour {
        [SerializeField] private SinglePlayerMenu singlePlayerMenu;
        [SerializeField] private InputField seedInput;
        [SerializeField] private Button defaultActiveButton;
        [SerializeField] private Button startButton;

        [CanBeNull] private LevelData _levelData;
        [SerializeField] private Dropdown locationDropdown;
        [SerializeField] private Dropdown environmentDropdown;

        private Animator _animator;

        private void Awake() {
            _animator = GetComponent<Animator>();
            
            Location.PopulateDropDown(locationDropdown, option => option.ToUpper());
            EnumExtensions.PopulateDropDownWithEnum<Environment>(environmentDropdown, option => option.ToUpper());
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        public void Show() {
            startButton.interactable = true;

            gameObject.SetActive(true);
            defaultActiveButton.Select();
            _animator.SetBool("Open", true);
        }

        public void ClosePanel() {
            UIAudioManager.Instance.Play("ui-cancel");
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
            startButton.interactable = false;
            
            var levelData = _levelData != null ? _levelData : new LevelData();
            levelData.location = Preferences.Instance.GetBool("enableExperimentalTerrain")
                ? Location.TerrainV2
                : Location.TerrainV1;
            levelData.gameType = GameType.FreeRoam;
            levelData.terrainSeed = seedInput.text;
            levelData.environment = (Environment)environmentDropdown.value;
            levelData.location = Location.FromId(locationDropdown.value);


            FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, levelData);
        }
    }
}