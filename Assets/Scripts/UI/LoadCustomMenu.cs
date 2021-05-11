using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Engine;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LoadCustomMenu : MonoBehaviour {
    public InputField mapInfo;
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
        saveInput.text = "";
        mapInfo.text = "";
        _levelData = null;
    }

    public void OnSaveInputFieldChanged(string levelString) {
        Debug.Log("TEXT CHANGE");
        _levelData = null;
        saveWarning.enabled = false;
        goButton.enabled = false;

        // simple fast checks to prevent it parsing for every character in a large paste operation
        var text = saveInput.text;
        if (text.Length > 0) {
            if (text.FirstOrDefault() == '{' && text.Last() == '}') {

                _levelData = LevelData.FromJsonString(text);

                if (_levelData == null || _levelData.version == 0) {
                    SetInvalidState();
                }
                else {
                    SetValidState();
                }
            }
            else {
                SetInvalidState();
            }
        }
    }

    public void StartMap() {
        var levelData = _levelData ?? new LevelData();
        Game.Instance.StartGame(levelData);
    }

    private void SetValidState() {
        Debug.Log("VALID");
        if (_levelData?.name.Length > 0 || _levelData?.terrainSeed.Length > 0) {
            mapInfo.text = _levelData.name;
            mapInfo.text += _levelData.terrainSeed.Length > 0 ? "    < SEED: " + _levelData.terrainSeed + " >" : "";
        }
        else {
            mapInfo.text = "<Unknown>";
        }
        saveWarning.enabled = false;
        goButton.enabled = true;
    }
    private void SetInvalidState() {
        Debug.Log("INVALID");
        mapInfo.text = "";
        saveWarning.enabled = true;
        goButton.enabled = false;
    }
}
