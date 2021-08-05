using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Player;
using Mirror;
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
            loadCustomButton.gameObject.SetActive(value);
            gameModeDropdown.gameObject.SetActive(value);
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
            gameModeClientLabel.text = _lobbyLevelData.GameTypeLabel.ToUpper();
            environmentClientLabel.text = _lobbyLevelData.EnvironmentLabel.ToUpper();
            locationClientLabel.text = _lobbyLevelData.LocationLabel.ToUpper();
        }
    }

    public void OnGameTypeChanged() {
        switch (gameModeDropdown.value) {
            case 0:
                _lobbyLevelData.gameType = GameType.FreeRoam;
                break;
            case 1:
                _lobbyLevelData.gameType = GameType.TimeTrial;
                break;
            case 2:
                _lobbyLevelData.gameType = GameType.HoonAttack;
                break;
        }
        UpdateLobby();
    }

    public void OnEnvironmentChanged() {
        switch (environmentDropdown.value) {
            case 0:
                _lobbyLevelData.environment = Environment.PlanetOrbitTop;
                break;
            case 1:
                _lobbyLevelData.environment = Environment.PlanetOrbitBottom;
                break;
            case 2:
                _lobbyLevelData.environment = Environment.SunriseClear;
                break;
            case 3:
                _lobbyLevelData.environment = Environment.NoonClear;
                break;
            case 4:
                _lobbyLevelData.environment = Environment.NoonCloudy;
                break;
            case 5:
                _lobbyLevelData.environment = Environment.NoonStormy;
                break;
            case 6:
                _lobbyLevelData.environment = Environment.SunsetClear;
                break;
            case 7:
                _lobbyLevelData.environment = Environment.SunsetCloudy;
                break;
            case 8:
                _lobbyLevelData.environment = Environment.NightClear;
                break;
            case 9:
                _lobbyLevelData.environment = Environment.NightCloudy;
                break;
        }
        UpdateLobby();
    }

    public void OnLocationChanged() {
        switch (locationDropdown.value) {
            case 0:
                _lobbyLevelData.location = Location.TestSpaceStation;
                break;
            case 1:
                _lobbyLevelData.location = Location.TerrainV1;
                break;
            case 2:
                _lobbyLevelData.location = Location.TerrainV2;
                break;
        }
        UpdateLobby();
    }

    private void UpdateLobby() {
        if (NetworkClient.isHostClient) {
            var host = LobbyPlayer.FindLocal;
            if (host) {
                host.UpdateLobby(LobbyLevelData);
            }
        }
    }
}
