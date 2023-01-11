using Core;
using Core.Player;
using Core.Scores;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameUI.GameModes {
    public class GameModeUIHandler : MonoBehaviour {
        [FormerlySerializedAs("screenText")] [SerializeField]
        private GameModeUIText gameModeUIText;

        [SerializeField] private HoldBoostButtonText holdBoostButtonText;
        [SerializeField] private RaceResultsScreen raceResultsScreen;

        public GameModeUIText GameModeUIText => gameModeUIText;
        public HoldBoostButtonText HoldBoostButtonText => holdBoostButtonText;
        public RaceResultsScreen RaceResultsScreen => raceResultsScreen;

        private void Awake() {
            HideResultsScreen();
        }

        private void OnEnable() {
            Game.OnRestart += OnReset;
        }

        private void OnDisable() {
            Game.OnRestart -= OnReset;
        }

        private void OnReset() {
            HoldBoostButtonText.Reset();
        }

        // Show and hide whatever game mode result screen there is
        public void ShowResultsScreen(Score score, Score previousBest, bool isValid, string replayFilename, string replayFilepath) {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                player.User.ShipCameraRig.SwitchToEndScreenCamera();

                // TODO: what the fuck does this have to do with UI?!
                player.User.DisableGameInput();
                player.User.pauseMenuEnabled = false;
                player.ShipPhysics.BringToStop();
            }

            raceResultsScreen.gameObject.SetActive(true);
            raceResultsScreen.Show(score, previousBest, isValid, replayFilename, replayFilepath);
        }

        public void HideResultsScreen() {
            raceResultsScreen.Hide();
        }
    }
}