using System;
using Audio;
using Core;
using Core.MapData;
using JetBrains.Annotations;
using Misc;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Environment = Core.MapData.Environment;

namespace Menus.Main_Menu {
    public class FreeRoamMenu : MenuBase {
        [SerializeField] private SinglePlayerMenu singlePlayerMenu;
        [SerializeField] private InputField seedInput;
        [SerializeField] private Button startButton;
        [SerializeField] private Dropdown locationDropdown;
        [SerializeField] private Dropdown environmentDropdown;
        [SerializeField] private Dropdown musicDropdown;
        [SerializeField] private Text locationDescriptionHeader;
        [SerializeField] private Text locationDescription;

        [CanBeNull] private LevelData _levelData;

        protected override void OnOpen() {
            var lastPlayedLocation = Preferences.Instance.GetString("lastPlayedFreeRoamLocation");
            var lastPlayedEnvironment = Preferences.Instance.GetString("lastPlayedFreeRoamEnvironment");
            var lastPlayedMusic = Preferences.Instance.GetString("lastPlayedFreeRoamMusic");

            var location = Location.FromString(lastPlayedLocation);
            var environment = Environment.FromString(lastPlayedEnvironment);
            var music = MusicTrack.FromString(lastPlayedMusic);

            FdEnum.PopulateDropDown(Location.List(), locationDropdown, option => option.Name.ToUpper());
            FdEnum.PopulateDropDown(Environment.List(), environmentDropdown, option => option.Name.ToUpper());
            FdEnum.PopulateDropDown(MusicTrack.List(), musicDropdown,
                option =>
                    (option.Artist != "" ? $"{option.Artist} - {option.Name}" : option.Name).ToUpper());

            locationDropdown.value = location.Id;
            environmentDropdown.value = environment.Id;
            musicDropdown.value = music.Id;

            locationDescriptionHeader.text = location.Name;
            locationDescription.text = location.Description;

            UpdateSeedField();
            _levelData = null;
        }

        public void ClosePanel() {
            if (MusicManager.Instance.CurrentPlayingTrack != MusicTrack.MainMenu)
                MusicManager.Instance.PlayMusic(MusicTrack.MainMenu, false, false, false);
            Cancel();
        }

        [UsedImplicitly]
        public void OnSeedInputFieldChanged(string seed) {
            if (seedInput.text.Length == 0) seedInput.text = Guid.NewGuid().ToString();
        }

        public void OnLocationChanged() {
            UpdateSeedField();
            var location = Location.FromId(locationDropdown.value);
            locationDescriptionHeader.text = location.Name;
            locationDescription.text = location.Description;
        }

        public void StartFreeRoam() {
            startButton.interactable = false;

            var levelData = _levelData ?? new LevelData();
            var location = Location.FromId(locationDropdown.value);
            var environment = Environment.FromId(environmentDropdown.value);
            var music = MusicTrack.FromId(musicDropdown.value);

            levelData.gameType = GameType.FreeRoam;
            levelData.terrainSeed = location.IsTerrain ? seedInput.text : null;
            levelData.location = location;
            levelData.environment = environment;
            levelData.musicTrack = music;

            Preferences.Instance.SetString("lastPlayedFreeRoamLocation", location.Name);
            Preferences.Instance.SetString("lastPlayedFreeRoamEnvironment", environment.Name);
            Preferences.Instance.SetString("lastPlayedFreeRoamMusic", music.Name);

            Preferences.Instance.Save();

            FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, levelData);
        }

        [UsedImplicitly]
        public void PreviewMusic(BaseEventData eventData) {
            var dropdownItem = eventData.selectedObject;
            var index = -1;
            var parent = dropdownItem.transform.parent;
            for (var i = 0; i < parent.childCount; i++) {
                var child = parent.GetChild(i);
                if (child.gameObject == dropdownItem)
                    index = i - 1;
            }

            MusicManager.Instance.PlayMusic(MusicTrack.FromId(index), false, false, true);
        }

        private void UpdateSeedField() {
            var location = Location.FromId(locationDropdown.value);
            seedInput.interactable = location.IsTerrain;
            seedInput.text = location.IsTerrain ? Guid.NewGuid().ToString() : "<LOCATION SEED NOT NEEDED>";
        }
    }
}