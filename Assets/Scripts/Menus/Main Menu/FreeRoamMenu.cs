using System;
using Audio;
using Core;
using Core.MapData;
using JetBrains.Annotations;
using Misc;
using UnityEngine;
using UnityEngine.UI;
using Environment = Core.MapData.Environment;


namespace Menus.Main_Menu {
    public class FreeRoamMenu : MenuBase {
        [SerializeField] private SinglePlayerMenu singlePlayerMenu;
        [SerializeField] private InputField seedInput;
        [SerializeField] private Button startButton;

        [CanBeNull] private LevelData _levelData;
        [SerializeField] private Dropdown locationDropdown;
        [SerializeField] private Dropdown environmentDropdown;

        protected override void OnOpen() {
            FdEnum.PopulateDropDown(Location.List(), locationDropdown, option => option.ToUpper());
            FdEnum.PopulateDropDown(Environment.List(), environmentDropdown, option => option.ToUpper());
            UpdateSeedField();
            _levelData = null;
        }
        
        public void ClosePanel() {
            Cancel();
        }

        public void OnSeedInputFieldChanged(string seed) {
            if (seedInput.text.Length == 0) {
                seedInput.text = Guid.NewGuid().ToString();
            }
        }

        public void OnLocationChanged() {
            UpdateSeedField();
        }

        public void StartFreeRoam() {
            startButton.interactable = false;
            
            var levelData = _levelData ?? new LevelData();
            var location = Location.FromId(locationDropdown.value);
            
            levelData.gameType = GameType.FreeRoam;
            levelData.terrainSeed = location.IsTerrain ? seedInput.text : "";
            levelData.environment = Environment.FromId(environmentDropdown.value);
            levelData.location = location;

            FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, levelData);
        }

        private void UpdateSeedField() {
            var location = Location.FromId(locationDropdown.value);
            seedInput.interactable = location.IsTerrain;
            seedInput.text = location.IsTerrain ? Guid.NewGuid().ToString() : "<LOCATION SEED NOT NEEDED>";
        }
    }
}