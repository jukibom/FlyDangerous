using System.Linq;
using Core;
using Core.MapData;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class LoadCustomMenu : MenuBase {
        [SerializeField] private InputField mapInfo;
        [SerializeField] private InputField saveInput;
        [SerializeField] private Text saveWarning;
        [SerializeField] private Button startButton;
        [CanBeNull] private LevelData _levelData;

        private void OnEnable() {
            saveInput.text = "";
            mapInfo.text = "";
            _levelData = null;
        }

        protected override void OnOpen() {
            OnSaveInputFieldChanged(saveInput.text);
        }

        public void ClosePanel() {
            Cancel();
        }

        public void OnSaveInputFieldChanged(string levelString) {
            _levelData = null;
            saveWarning.enabled = false;

            // simple fast checks to prevent it parsing for every character in a large paste operation
            SetInvalidState();

            var text = saveInput.text;
            if (text.Length > 0)
                if (text.FirstOrDefault() == '{' && text.Last() == '}') {
                    _levelData = LevelData.FromJsonString(text);

                    if (_levelData != null && _levelData.version != 0) SetValidState();
                }

            saveWarning.enabled = text.Length != 0 && saveWarning.enabled;
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
            startButton.interactable = true;
        }

        private void SetInvalidState() {
            mapInfo.text = "";
            saveWarning.enabled = true;
            startButton.interactable = false;
        }
    }
}