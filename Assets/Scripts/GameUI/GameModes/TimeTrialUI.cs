using Core.Player;
using Core.Scores;
using UnityEngine;

namespace GameUI.GameModes {
    public interface IGameModeUI {
        // ReSharper disable once InconsistentNaming (used as mono behaviour interface)
        public GameObject gameObject { get; }

        public Timers Timers { get; }

        // Show and hide the game mode specific UI 
        void ShowMainUI();
        void HideMainUI();

        // Show and hide whatever game mode result screen there is
        void ShowResultsScreen(Score score, Score previousBest);
        void HideResultsScreen();
    }

    public class TimeTrialUI : MonoBehaviour, IGameModeUI {
        [SerializeField] private Timers timers;
        [SerializeField] private RaceResultsScreen raceResultsScreen;

        private void Awake() {
            raceResultsScreen.Hide();
        }

        public Timers Timers => timers;

        public void ShowMainUI() {
            timers.gameObject.SetActive(true);
        }

        public void HideMainUI() {
            timers.gameObject.SetActive(false);
        }

        public void ShowResultsScreen(Score score, Score previousBest = null) {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                player.User.ShipCameraRig.SwitchToEndScreenCamera();
                player.User.DisableGameInput();
                player.User.EnableGameInput();
            }

            HideMainUI();
            raceResultsScreen.gameObject.SetActive(true);
            raceResultsScreen.Show(score, previousBest);
        }

        public void HideResultsScreen() {
            raceResultsScreen.gameObject.SetActive(false);
        }
    }
}