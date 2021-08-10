using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Core;
using Core.Player;
using Mirror;
using Misc;
using UnityEngine;
using UnityEngine.UI;
using Environment = Core.Environment;

public class LobbyConfigurationPanel : MonoBehaviour
{
    [SerializeField] private Button loadCustomButton;
    [SerializeField] private Dropdown gameModeDropdown;
    [SerializeField] private Dropdown environmentDropdown;
    [SerializeField] private Dropdown locationDropdown;
    
    // clients see labels
    [SerializeField] private Text gameModeClientLabel;
    [SerializeField] private Text environmentClientLabel;
    [SerializeField] private Text locationClientLabel;

    public bool IsHost {
        set {
            // TODO
            loadCustomButton.gameObject.SetActive(false);
            gameModeDropdown.gameObject.SetActive(value);
            gameModeDropdown.interactable = false;  // TODO
            
            environmentDropdown.gameObject.SetActive(value);
            locationDropdown.gameObject.SetActive(value);
        
            gameModeClientLabel.gameObject.SetActive(!value);
            environmentClientLabel.gameObject.SetActive(!value);
            locationClientLabel.gameObject.SetActive(!value);
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
            
            // client-side non-editable fields
            gameModeClientLabel.text = EnumExtensions.DescriptionAtt(_lobbyLevelData.gameType).ToUpper();
            environmentClientLabel.text = EnumExtensions.DescriptionAtt(_lobbyLevelData.environment).ToUpper();
            locationClientLabel.text = EnumExtensions.DescriptionAtt(_lobbyLevelData.location).ToUpper();
            
            // TODO (this is reactivated by setting the data...)
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
        _lobbyLevelData.gameType = (GameType)gameModeDropdown.value;
        _lobbyLevelData.environment = (Environment)environmentDropdown.value;
        _lobbyLevelData.location = (Location)locationDropdown.value;

        UpdateLobby();
    }

    private void UpdateLobby() {
        var localPlayer = LobbyPlayer.FindLocal;
        if (localPlayer && localPlayer.isHost) {
            localPlayer.UpdateLobby(LobbyLevelData);
        }
    }
    
}
