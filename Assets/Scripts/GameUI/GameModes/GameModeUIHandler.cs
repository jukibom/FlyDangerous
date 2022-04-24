using Core;
using Core.MapData;
using UnityEngine;

namespace GameUI.GameModes {
    public class GameModeUIHandler : MonoBehaviour {
        [SerializeField] private TimeTrialUI timeTrialUI;

        public IGameModeUI ActiveGameModeUI { get; private set; }

        private void Awake() {
            timeTrialUI.gameObject.SetActive(false);
        }

        private void OnEnable() {
            Game.OnRestart += OnReset;
        }

        private void OnDisable() {
            Game.OnRestart -= OnReset;
        }

        public void SetGameMode(GameType gameType) {
            if (gameType.Id == GameType.TimeTrial.Id) {
                ActiveGameModeUI = timeTrialUI;
                ActiveGameModeUI.gameObject.SetActive(true);
            }
        }

        private void OnReset() {
            ActiveGameModeUI?.HideResultsScreen();
            ActiveGameModeUI?.ShowMainUI();
        }
    }
}