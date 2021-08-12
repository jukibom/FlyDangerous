using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Core;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class LoadCustomMenu : MonoBehaviour {
        [SerializeField] private SinglePlayerMenu singlePlayerMenu;
        [SerializeField] private InputField mapInfo;
        [SerializeField] private InputField saveInput;
        [SerializeField] private Text saveWarning;
        [SerializeField] private Button defaultActiveButton;
        [SerializeField] private Button startButton;

        [CanBeNull] private LevelData _levelData;

        private Animator _animator;

        private void Awake() {
            this._animator = this.GetComponent<Animator>();
        }

        public void Hide() {
            this.gameObject.SetActive(false);
        }

        public void Show() {
            startButton.interactable = true;
            gameObject.SetActive(true);
            defaultActiveButton.Select();
            _animator.SetBool("Open", true);
        }
        
        public void ClosePanel() {
            AudioManager.Instance.Play("ui-cancel");
            singlePlayerMenu.Show();
            Hide();
        }

        private void OnEnable() {
            saveInput.text = "";
            mapInfo.text = "";
            _levelData = null;
        }

        public void OnSaveInputFieldChanged(string levelString) {
            _levelData = null;
            saveWarning.enabled = false;
            defaultActiveButton.enabled = false;

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
            startButton.interactable = false;
            var levelData = _levelData ?? new LevelData();
            FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, levelData);
        }

        private void SetValidState() {
            if (_levelData?.name.Length > 0 || _levelData?.terrainSeed.Length > 0) {
                mapInfo.text = _levelData.name;
                mapInfo.text += _levelData.terrainSeed.Length > 0 ? "    < SEED: " + _levelData.terrainSeed + " >" : "";
            }
            else {
                mapInfo.text = "<Unknown>";
            }

            saveWarning.enabled = false;
            defaultActiveButton.enabled = true;
        }

        private void SetInvalidState() {
            mapInfo.text = "";
            saveWarning.enabled = true;
            defaultActiveButton.enabled = false;
        }
    }
}