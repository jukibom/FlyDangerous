using System;
using System.Globalization;
using Core;
using Core.MapData;
using Core.Player;
using Misc;
using UnityEngine;
using UnityEngine.UI;
using Environment = Core.MapData.Environment;

namespace Menus.Main_Menu.Components {
    public class LobbyConfigurationPanel : MonoBehaviour {
        [SerializeField] private Button loadCustomButton;
        [SerializeField] private Dropdown gameModeDropdown;
        [SerializeField] private Dropdown environmentDropdown;
        [SerializeField] private Dropdown locationDropdown;
        [SerializeField] private InputField maxPlayersInputField;
        [SerializeField] private Text passwordLabel;
        [SerializeField] private InputField passwordInputField;

        // clients see labels
        [SerializeField] private Text gameModeClientLabel;
        [SerializeField] private Text environmentClientLabel;
        [SerializeField] private Text locationClientLabel;
        [SerializeField] private Text maxPlayersLabel;

        public short maxPlayers;

        private LevelData _lobbyLevelData;

        public bool IsHost {
            set {
                // host-only interactive config elements
                loadCustomButton.gameObject.SetActive(false);
                gameModeDropdown.gameObject.SetActive(value);
                gameModeDropdown.interactable = false; // TODO
                environmentDropdown.gameObject.SetActive(value);
                locationDropdown.gameObject.SetActive(value);
                maxPlayersInputField.gameObject.SetActive(value);
                passwordLabel.gameObject.SetActive(value);
                passwordInputField.gameObject.SetActive(value);

                // client-side read-only label elements
                gameModeClientLabel.gameObject.SetActive(!value);
                environmentClientLabel.gameObject.SetActive(!value);
                locationClientLabel.gameObject.SetActive(!value);
                maxPlayersLabel.gameObject.SetActive(!value);
            }
        }

        public LevelData LobbyLevelData {
            get => _lobbyLevelData;
            set {
                _lobbyLevelData = value;

                // ridiculous looping dropdown on change event avoidance
                passwordInputField.text = FdNetworkManager.serverPassword;
                var gameModeValue = _lobbyLevelData.gameType.Id;
                var environmentValue = _lobbyLevelData.environment.Id;
                var locationValue = _lobbyLevelData.location.Id;

                // host drop-downs
                gameModeDropdown.value = gameModeValue;
                environmentDropdown.value = environmentValue;
                locationDropdown.value = locationValue;
                maxPlayersInputField.text = maxPlayers.ToString();

                // client-side non-editable fields
                gameModeClientLabel.text = _lobbyLevelData.gameType.Name.ToUpper();
                environmentClientLabel.text = _lobbyLevelData.environment.Name.ToUpper();
                locationClientLabel.text = _lobbyLevelData.location.Name.ToUpper();
                maxPlayersLabel.text = maxPlayers.ToString();

                // TODO: other game modes than free play (this is reactivated by setting the data...)
                gameModeDropdown.interactable = false;
            }
        }

        private void Awake() {
            FdEnum.PopulateDropDown(GameType.List(), gameModeDropdown, option => option.ToUpper());
            FdEnum.PopulateDropDown(Environment.List(), environmentDropdown, option => option.ToUpper());
            FdEnum.PopulateDropDown(Location.List(), locationDropdown, option => option.ToUpper());

            // resume where we left on on lobby creation (if client, this is overwritten by message)
            LobbyLevelData = Game.Instance.LoadedLevelData;
            UpdateLobby();
        }

        public void OnConfigurationSettingChanged() {
            // server password
            FdNetworkManager.serverPassword = passwordInputField.text;

            // parse out max players text box (between 2 and maxPlayerLimit)
            maxPlayers = short.Parse(maxPlayersInputField.text, CultureInfo.InvariantCulture);
            maxPlayersInputField.text = maxPlayers.ToString();
            FdNetworkManager.Instance.maxPlayers = maxPlayers;

            _lobbyLevelData.gameType = GameType.FromId(gameModeDropdown.value);
            _lobbyLevelData.environment = Environment.FromId(environmentDropdown.value);
            _lobbyLevelData.location = Location.FromId(locationDropdown.value);

            UpdateLobby();
        }

        public void ClampMaxPlayersInput() {
            try {
                var value = Math.Min(Math.Max(float.Parse(maxPlayersInputField.text, CultureInfo.InvariantCulture), 1), FdNetworkManager.maxPlayerLimit);
                maxPlayersInputField.text = value.ToString("0");
            }
            catch {
                maxPlayersInputField.text = "2";
            }
        }

        private void UpdateLobby() {
            var localPlayer = FdPlayer.FindLocalLobbyPlayer;
            if (localPlayer && localPlayer.isHost) localPlayer.UpdateLobby(LobbyLevelData, maxPlayers);
        }
    }
}