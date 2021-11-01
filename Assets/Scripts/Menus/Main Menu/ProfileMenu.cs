using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Core;
using Core.Ship;
using Menus.Main_Menu.Components;
using Menus.Pause_Menu;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace Menus.Main_Menu {
    public class ProfileMenu : MenuBase {

        [SerializeField] private TopMenu topMenu;

        [SerializeField] private InputField playerNameTextField;
        [SerializeField] private Dropdown countryDropdown;
        
        [SerializeField] private ShipSelectionRenderer shipSelectionRenderer;
        [SerializeField] private Text shipName;
        [SerializeField] private Text shipDescription;

        [SerializeField] private Text shipCounter;
        [SerializeField] private UIButton nextButton;
        [SerializeField] private UIButton prevButton;

        [SerializeField] private Thruster thruster;
        [SerializeField] private Image trailPreview;
        [SerializeField] private Image lightPreview;

        [SerializeField] private FlexibleColorPicker playerShipPrimaryColorPicker;
        [SerializeField] private FlexibleColorPicker playerShipAccentColorPicker;
        [SerializeField] private FlexibleColorPicker playerShipThrusterColorPicker;
        [SerializeField] private FlexibleColorPicker playerShipTrailColorPicker;
        [SerializeField] private FlexibleColorPicker playerShipHeadLightsColorPicker;
        
        private string _playerShipPrimaryColor;
        private string _playerShipAccentColor;
        private string _playerShipThrusterColor;
        private string _playerShipTrailColor;
        private string _playerShipHeadLightsColor;
        
        private List<ShipMeta> _ships;
        private ShipMeta _selectedShip;
        private bool _ready;
        
        protected override void OnOpen() {
            _ships = ShipMeta.List().ToList();
            LoadFromPreferences();
        }

        private void FixedUpdate() {
            // wait for color pickers to be set god damnit
            if (_ready) {
                bool shouldUpdate = false;

                var playerShipPrimaryColor = $"#{ColorUtility.ToHtmlStringRGB(playerShipPrimaryColorPicker.color)}";
                var playerShipAccentColor = $"#{ColorUtility.ToHtmlStringRGB(playerShipAccentColorPicker.color)}";
                var playerShipThrusterColor = $"#{ColorUtility.ToHtmlStringRGB(playerShipThrusterColorPicker.color)}";
                var playerShipTrailColor = $"#{ColorUtility.ToHtmlStringRGB(playerShipTrailColorPicker.color)}";
                var playerShipHeadLightsColor =
                    $"#{ColorUtility.ToHtmlStringRGB(playerShipHeadLightsColorPicker.color)}";

                if (playerShipPrimaryColor != _playerShipPrimaryColor) {
                    _playerShipPrimaryColor = playerShipPrimaryColor;
                    shouldUpdate = true;
                }

                if (playerShipAccentColor != _playerShipAccentColor) {
                    _playerShipAccentColor = playerShipAccentColor;
                    shouldUpdate = true;
                }

                if (playerShipThrusterColor != _playerShipThrusterColor) {
                    _playerShipThrusterColor = playerShipThrusterColor;
                    shouldUpdate = true;
                }

                if (playerShipTrailColor != _playerShipTrailColor) {
                    _playerShipTrailColor = playerShipTrailColor;
                    shouldUpdate = true;
                }

                if (playerShipHeadLightsColor != _playerShipHeadLightsColor) {
                    _playerShipHeadLightsColor = playerShipHeadLightsColor;
                    shouldUpdate = true;
                }

                if (shouldUpdate) {
                    RefreshColors();
                }
            }
        }

        public void Apply() {
            Preferences.Instance.SetString("playerName", playerNameTextField.text);
            // TODO: Region
            Preferences.Instance.SetString("playerShipDesign", _selectedShip.Name);
            Preferences.Instance.SetString("playerShipPrimaryColor", _playerShipPrimaryColor);
            Preferences.Instance.SetString("playerShipAccentColor", _playerShipAccentColor);
            Preferences.Instance.SetString("playerShipThrusterColor", _playerShipThrusterColor);
            Preferences.Instance.SetString("playerShipTrailColor", _playerShipTrailColor);
            Preferences.Instance.SetString("playerShipHeadLightsColor", _playerShipHeadLightsColor);
            Preferences.Instance.Save();
            // we're going backward but with a positive apply sound so don't set the call chain in the previous menu
            Progress(caller, false, false);
        }

        public void NextShip() {
            var index = _ships.FindIndex(ship => ship.Id == _selectedShip.Id);
            if (_ships.Count > index + 1) {
                SetShip(_ships[index + 1]);
            }
        }

        public void PrevShip() {
            var index = _ships.FindIndex(ship => ship.Id == _selectedShip.Id);
            if (index > 0) {
                SetShip(_ships[index - 1]);
            }
        }
        
        public void SetShipPrimaryColor(string htmlColor) {
            _playerShipPrimaryColor = htmlColor;
            shipSelectionRenderer.SetShipPrimaryColor(htmlColor);
        }
        
        public void SetShipAccentColor(string htmlColor) {
            _playerShipAccentColor = htmlColor;
            shipSelectionRenderer.SetShipAccentColor(htmlColor);
        }
        
        public void SetThrusterColor(string htmlColor) {
            _playerShipThrusterColor = htmlColor;
            thruster.ThrustColor = ParseColor(htmlColor);
        }

        public void SetTrailColor(string htmlColor) {
            _playerShipTrailColor = htmlColor;
            trailPreview.color = ParseColor(htmlColor);
        }

        public void SetShipLightColor(string htmlColor) {
            _playerShipHeadLightsColor = htmlColor;
            lightPreview.color = ParseColor(htmlColor);
        }

        private void LoadFromPreferences() {
            IEnumerator Load() {
                // color picker has some weird nonsense logic if called before it's start (which is 2 frames from now)
                // and it sometimes fails to set the color because god knows why but it really doesn't matter
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                // load details from prefs
                // TODO: region
                playerNameTextField.text = Preferences.Instance.GetString("playerName");
                _playerShipPrimaryColor = Preferences.Instance.GetString("playerShipPrimaryColor");
                _playerShipAccentColor = Preferences.Instance.GetString("playerShipAccentColor");
                _playerShipThrusterColor = Preferences.Instance.GetString("playerShipThrusterColor");
                _playerShipTrailColor = Preferences.Instance.GetString("playerShipTrailColor");
                _playerShipHeadLightsColor = Preferences.Instance.GetString("playerShipHeadLightsColor");

                playerShipPrimaryColorPicker.color = ParseColor(_playerShipPrimaryColor);
                playerShipAccentColorPicker.color = ParseColor(_playerShipAccentColor);
                playerShipThrusterColorPicker.color = ParseColor(_playerShipThrusterColor);
                playerShipTrailColorPicker.color = ParseColor(_playerShipTrailColor);
                playerShipHeadLightsColorPicker.color = ParseColor(_playerShipHeadLightsColor);

                SetShip(ShipMeta.FromString(Preferences.Instance.GetString("playerShipDesign")));
                _ready = true;
            }

            StartCoroutine(Load());
        }
        private void SetShip(ShipMeta ship) {
            _selectedShip = ship;
            shipSelectionRenderer.SetShip(ship);
            shipName.text = ship.FullName;
            shipDescription.text = ship.Description;
            
            UpdateShipSelectionButtonState();
            UpdateShipCounter();
            RefreshColors();
        }

        private void RefreshColors() {
            SetShipPrimaryColor(_playerShipPrimaryColor);
            SetShipAccentColor(_playerShipAccentColor);
            SetThrusterColor(_playerShipThrusterColor);
            SetTrailColor(_playerShipTrailColor);
            SetShipLightColor(_playerShipHeadLightsColor);
        }

        // if there's no more ships to the left or right of the data structure, disable those buttons
        private void UpdateShipSelectionButtonState() {
            prevButton.button.interactable = true;
            nextButton.button.interactable = true;
            if (_selectedShip.Id == 0) {
                prevButton.button.interactable = false;
            }

            if (_selectedShip.Id == _ships.Count - 1) {
                nextButton.button.interactable = false;
            }
        }

        private void UpdateShipCounter() {
            shipCounter.text = $"{_selectedShip.Id + 1} of {_ships.Count}";
        }

        Color ParseColor(string htmlColor) {
            if (!ColorUtility.TryParseHtmlString(htmlColor, out var color)) {
                color = Color.red;
                Debug.LogWarning("Failed to parse html color " + htmlColor);
            }

            return color;
        }
    }
}