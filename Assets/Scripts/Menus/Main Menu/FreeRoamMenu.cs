using System;
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
        [SerializeField] private Dropdown locationDropdown;
        [SerializeField] private Dropdown environmentDropdown;

        [CanBeNull] private LevelData _levelData;

        protected override void OnOpen() {
            var lastPlayedLocation = Preferences.Instance.GetString("lastPlayedFreeRoamLocation");
            var lastPlayedEnvironment = Preferences.Instance.GetString("lastPlayedFreeRoamEnvironment");

            var location = Location.FromString(lastPlayedLocation);
            var environment = Environment.FromString(lastPlayedEnvironment);

            FdEnum.PopulateDropDown(Location.List(), locationDropdown, option => option.ToUpper());
            FdEnum.PopulateDropDown(Environment.List(), environmentDropdown, option => option.ToUpper());

            locationDropdown.value = location.Id;
            environmentDropdown.value = environment.Id;

            UpdateSeedField();
            _levelData = null;
        }

        public void ClosePanel() {
            Cancel();
        }

        [UsedImplicitly]
        public void OnSeedInputFieldChanged(string seed) {
            if (seedInput.text.Length == 0) seedInput.text = Guid.NewGuid().ToString();
        }

        public void OnLocationChanged() {
            UpdateSeedField();
        }

        public void StartFreeRoam() {
            startButton.interactable = false;

            var levelData = _levelData ?? new LevelData();
            var location = Location.FromId(locationDropdown.value);
            var environment = Environment.FromId(environmentDropdown.value);

            levelData.gameType = GameType.FreeRoam;
            levelData.terrainSeed = location.IsTerrain ? seedInput.text : "";
            levelData.location = location;
            levelData.environment = environment;

            Preferences.Instance.SetString("lastPlayedFreeRoamLocation", location.Name);
            Preferences.Instance.SetString("lastPlayedFreeRoamEnvironment", environment.Name);
            Preferences.Instance.Save();

            FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, levelData);
        }

        private void UpdateSeedField() {
            var location = Location.FromId(locationDropdown.value);
            seedInput.interactable = location.IsTerrain;
            seedInput.text = location.IsTerrain ? Guid.NewGuid().ToString() : "<LOCATION SEED NOT NEEDED>";
        }
    }
}