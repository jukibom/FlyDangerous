using System;
using UnityEngine;

namespace GameUI.GameModes {

    public interface IGameModeUI {
        // ReSharper disable once InconsistentNaming (used as mono behaviour interface)
        public GameObject gameObject { get; }

        public Timers Timers { get; }
        void ShowMainUI();
        void HideMainUI();
        void ShowResultsScreen();
        void HideResultsScreen();
    }
    
    public class TimeTrialUI : MonoBehaviour, IGameModeUI {
        [SerializeField] private Timers timers;
        [SerializeField] private RaceResultsScreen raceResultsScreen;

        public Timers Timers => timers;
        
        private void Awake() {
            raceResultsScreen.Hide();
        }

        public void ShowMainUI() {
            timers.gameObject.SetActive(true);
        }

        public void HideMainUI() {
            timers.gameObject.SetActive(false);
        }

        public void ShowResultsScreen() {
            raceResultsScreen.gameObject.SetActive(true);
        }

        public void HideResultsScreen() {
            raceResultsScreen.gameObject.SetActive(false);
        }
    }
}
