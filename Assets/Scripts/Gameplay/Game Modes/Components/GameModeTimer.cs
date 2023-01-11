using Gameplay.Game_Modes.Components.Interfaces;
using UnityEngine;

namespace Gameplay.Game_Modes.Components {
    /**
     * Keeps track of current active time in a running session.
     * Can be used to generate splits too.
     */
    public class GameModeTimer : IGameModeTimer {
        public float CurrentSessionTimeSeconds { get; private set; }
        public float StartTimeSeconds { get; private set; }

        private bool _started;

        public void Reset() {
            CurrentSessionTimeSeconds = 0;
        }

        public void Start(IGameMode gameMode) {
            float startAt = 0;
            if (gameMode is IGameModeWithCountdown gameModeWithCountdown)
                startAt = -gameModeWithCountdown.StartingCountdownTime;
            Start(startAt);
        }

        public void Start(float fromSeconds = 0) {
            _started = true;
            CurrentSessionTimeSeconds = fromSeconds;
            StartTimeSeconds = Time.fixedTime;
        }

        public void Stop() {
            _started = false;
        }

        public void Tick(float tick) {
            if (_started) CurrentSessionTimeSeconds += tick;
        }
    }
}