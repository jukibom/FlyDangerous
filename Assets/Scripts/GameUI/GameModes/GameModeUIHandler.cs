using Core.MapData;
using UnityEngine;

namespace GameUI.GameModes {
    public class GameModeUIHandler : MonoBehaviour {
        [SerializeField] private TimeTrialUI timeTrialUI;

        public IGameModeUI ActiveGameModeUI { get; private set; }

        private void Awake() {
            timeTrialUI.gameObject.SetActive(false);
        }

        public void SetGameMode(GameType gameType) {
            if (gameType.Id == GameType.TimeTrial.Id) {
                ActiveGameModeUI = timeTrialUI;
                ActiveGameModeUI.gameObject.SetActive(true);
            }
        }
    }
}