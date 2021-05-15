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
    public Button goButton;

    [CanBeNull] private LevelData _levelData;
    [SerializeField] private Dropdown conditionsSelector;

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
        _levelData = null;
    }
    
    public void OnSeedInputFieldChanged(string seed) {
        if (seedInput.text.Length == 0) {
            seedInput.text = Guid.NewGuid().ToString();
        }
    }

    public void StartFreeRoam() {
        var levelData = _levelData != null ? _levelData : new LevelData();
        levelData.location = Location.Terrain;
        levelData.raceType = RaceType.None;
        levelData.terrainSeed = seedInput.text;

        // TODO: some better initial placement system for terrain in Game class (need to know when terrain has loaded)
        bool dynamicPlacementStart = true;
        if (dynamicPlacementStart) {
            levelData.startPosition.y = levelData.startPosition.y == 0 ? 2100 : levelData.startPosition.y;
            dynamicPlacementStart = false;
        }

        switch (conditionsSelector.value) {
            case 0: levelData.conditions = Conditions.NoonClear; break;
            case 1: levelData.conditions = Conditions.NightClear; break;
        }

        Game.Instance.StartGame(levelData, dynamicPlacementStart);
    }    
}
