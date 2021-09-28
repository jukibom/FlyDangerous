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
    public class FreeRoamMenu : MonoBehaviour {
        [SerializeField] private SinglePlayerMenu singlePlayerMenu;
        [SerializeField] private InputField seedInput;
        [SerializeField] private Button defaultActiveButton;
        [SerializeField] private Button startButton;

        [CanBeNull] private LevelData _levelData;
        [SerializeField] private Dropdown locationDropdown;
        [SerializeField] private Dropdown environmentDropdown;

        private Animator _animator;
        private static readonly int open = Animator.StringToHash("Open");

        private void Awake() {
            _animator = GetComponent<Animator>();
            
            FdEnum.PopulateDropDown(Location.List(), locationDropdown, option => option.ToUpper());
            FdEnum.PopulateDropDown(Environment.List(), environmentDropdown, option => option.ToUpper());
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        public void Show() {
            startButton.interactable = true;

            gameObject.SetActive(true);
            defaultActiveButton.Select();
            UpdateSeedField();
            _animator.SetBool(open, true);
        }

        public void ClosePanel() {
            UIAudioManager.Instance.Play("ui-cancel");
            singlePlayerMenu.Show();
            Hide();
        }

        private void OnEnable() {
            _levelData = null;
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
            levelData.gameType = GameType.FreeRoam;
            levelData.terrainSeed = seedInput.text;
            levelData.environment = Environment.FromId(environmentDropdown.value);
            levelData.location = Location.FromId(locationDropdown.value);

            FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, levelData);
        }

        private void UpdateSeedField() {
            var location = Location.FromId(locationDropdown.value);
            seedInput.interactable = location.IsTerrain;
            seedInput.text = location.IsTerrain ? Guid.NewGuid().ToString() : "<LOCATION SEED NOT NEEDED>";
        }
    }
}