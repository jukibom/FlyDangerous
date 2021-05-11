using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Engine;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FreeRoamMenu : MonoBehaviour {
    public InputField seedInput;
    public InputField saveInput;
    public Text saveWarning;
    public Button goButton;

    [CanBeNull] private LevelData _levelData;

    private Animator _animator;
        
    private void Awake() {
        this._animator = this.GetComponent<Animator>();
    }

    public void Hide() {
        this.gameObject.SetActive(false);
    }

    public void Show() {
        this.gameObject.SetActive(true);
        goButton.Select();
        this._animator.SetBool("Open", true);
    }
    
    private void OnEnable() {
        seedInput.text = Guid.NewGuid().ToString();
        saveInput.text = "";
        _levelData = null;
    }
    
    public void OnSeedInputFieldChanged(string seed) {
        if (seedInput.text.Length == 0) {
            seedInput.text = Guid.NewGuid().ToString();
        }

        if (_levelData != null && _levelData.terrainSeed != seedInput.text) {
            saveInput.text = "";
        }

        OnSaveInputFieldChanged("");
    }

    public void OnSaveInputFieldChanged(string levelString) {
        var text = saveInput.text;
        saveWarning.gameObject.SetActive(false);
        goButton.enabled = true;

        // simple fast checks to prevent it parsing for every character in a large paste operation
        if (text.Length > 0 && text.FirstOrDefault() == '{' && text.Last() == '}') {
            _levelData = LevelData.FromJsonString(text);

            if (_levelData == null) {
                saveWarning.enabled = true;
                saveWarning.gameObject.SetActive(true);
                goButton.enabled = false;
            }
            else {
                seedInput.text = _levelData.terrainSeed;
            }
        }
    }

    public void StartFreeRoam() {
        bool dynamicPlacementStart = _levelData == null;
        var levelData = _levelData != null ? _levelData : new LevelData();
        // TODO: this should come from the data really, implications?
        levelData.location = Location.Terrain;
        levelData.raceType = RaceType.FreeRoam;
        levelData.terrainSeed = seedInput.text;

        // TODO: some better initial placement system for terrain
        if (dynamicPlacementStart) {
            levelData.startPosition.y = levelData.startPosition.y == 0 ? 2100 : levelData.startPosition.y;
            dynamicPlacementStart = false;
        }
            
        Game.Instance.StartGame(levelData, dynamicPlacementStart);
    }    
}
