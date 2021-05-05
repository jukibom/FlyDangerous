using System;
using System.Collections;
using System.Collections.Generic;
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
        this._animator.SetBool("Open", true);
    }
    
    private void OnEnable() {
        seedInput.text = Guid.NewGuid().ToString();
    }
    
    public void OnSeedInputFieldChanged(string seed) {
        if (seedInput.text.Length == 0) {
            seedInput.text = Guid.NewGuid().ToString();
        }

        saveInput.text = "";
        OnSaveInputFieldChanged("");
    }

    public void OnSaveInputFieldChanged(string levelString) {
        
        // TODO: parse save data here
            // if error set warning text
            // if not, replace seed in text field
        if (saveInput.text.Length != 0) {
            saveWarning.gameObject.SetActive(true);
            goButton.enabled = false;
        }
        else {
            saveWarning.gameObject.SetActive(false);
            goButton.enabled = true;
        }
    }

    public void StartFreeRoam() {
        var levelData = _levelData != null ? _levelData : new LevelData();
        levelData.location = Location.Terrain;
        levelData.terrainSeed = seedInput.text;
            
        // TODO: get data from dialog
            
        // TODO: some better initial placement system for terrain
        levelData.startPosition.y = levelData.startPosition.y == 0 ? 2100 : levelData.startPosition.y;
            
        Game.Instance.StartGame(levelData);
    }    
}
