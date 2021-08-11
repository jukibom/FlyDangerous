using System;
using System.Globalization;
using Core;
using Core.Player;
using Misc;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Environment = Core.Environment;

public class LobbyConfigurationPanel : MonoBehaviour
{
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
    
    public bool IsHost {
        set {
            // host-only interactive config elements
            loadCustomButton.gameObject.SetActive(false);
            gameModeDropdown.gameObject.SetActive(value);
            gameModeDropdown.interactable = false;  // TODO
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

    private LevelData _lobbyLevelData = new LevelData {
        gameType = GameType.FreeRoam,
        environment = Environment.SunriseClear,
        location = Location.TerrainV2,
        terrainSeed = Guid.NewGuid().ToString(),
    };
    
    public LevelData LobbyLevelData {
        get => _lobbyLevelData;
        set {
            _lobbyLevelData = value;
            
            // ridiculous looping dropdown on change event avoidance
            var gameModeValue = (int) _lobbyLevelData.gameType;
            var environmentValue =  (int) _lobbyLevelData.environment;
            var locationValue = (int) _lobbyLevelData.location;

            // host drop-downs
            gameModeDropdown.value = gameModeValue;
            environmentDropdown.value = environmentValue;
            locationDropdown.value = locationValue;
            maxPlayersInputField.text = maxPlayers.ToString();
            
            // client-side non-editable fields
            gameModeClientLabel.text = EnumExtensions.DescriptionAtt(_lobbyLevelData.gameType).ToUpper();
            environmentClientLabel.text = EnumExtensions.DescriptionAtt(_lobbyLevelData.environment).ToUpper();
            locationClientLabel.text = EnumExtensions.DescriptionAtt(_lobbyLevelData.location).ToUpper();
            maxPlayersLabel.text = maxPlayers.ToString();

            // TODO: other game modes than free play (this is reactivated by setting the data...)
            gameModeDropdown.interactable = false;
        }
    }

    private void Awake() {
        EnumExtensions.PopulateDropDownWithEnum<GameType>(gameModeDropdown, option => option.ToUpper());
        EnumExtensions.PopulateDropDownWithEnum<Environment>(environmentDropdown, option => option.ToUpper());
        EnumExtensions.PopulateDropDownWithEnum<Location>(locationDropdown, option => option.ToUpper());
        
        // resume where we left on on lobby creation (if client, this is overwritten by message)
        LobbyLevelData = Game.Instance.LoadedLevelData;
        UpdateLobby();
    }

    public void OnConfigurationSettingChanged() {
        // parse out max players text box (between 2 and maxPlayerLimit)
        maxPlayers = Int16.Parse(maxPlayersInputField.text);
        maxPlayersInputField.text = maxPlayers.ToString();
        FdNetworkManager.Instance.maxPlayers = maxPlayers;
        
        _lobbyLevelData.gameType = (GameType)gameModeDropdown.value;
        _lobbyLevelData.environment = (Environment)environmentDropdown.value;
        _lobbyLevelData.location = (Location)locationDropdown.value;
        
        UpdateLobby();
    }

    public void ClampMaxPlayersInput() {
        try {
            var value = Math.Min(Math.Max(float.Parse(maxPlayersInputField.text), 1), FdNetworkManager.maxPlayerLimit);
            maxPlayersInputField.text = value.ToString("0");
        }
        catch {
            maxPlayersInputField.text = "2";
        }
    }

    private void UpdateLobby() {
        var localPlayer = LobbyPlayer.FindLocal;
        if (localPlayer && localPlayer.isHost) {
            localPlayer.UpdateLobby(LobbyLevelData, maxPlayers);
        }
    }
}
