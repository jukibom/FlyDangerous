using System;
using Core.Player;
using Gameplay.Game_Modes.Components.Interfaces;

namespace Gameplay.Game_Modes.Components {
    /**
     * Control the lifecycle of the game.
     * Restart and Complete must be called whenever game mode logic dictates in order to reset all modules correctly.
     * Do NOT call these as part of IGameModes' OnComplete or OnRestart functions as these will be called when executing
     * this and will  run into a stack overflow.
     */
    public class GameModeLifecycle : IGameModeLifecycle {
        private readonly GameModeHandler _gameModeHandler;
        private readonly ShipPlayer _shipPlayer;
        private readonly Action _onRestart;
        private readonly Action _onComplete;

        public bool IsShipActive => _gameModeHandler.ShipActive;
        public bool HasStarted => _gameModeHandler.HasStarted;

        public GameModeLifecycle(GameModeHandler gameModeHandler, ShipPlayer shipPlayer, Action onRestart, Action onComplete) {
            _gameModeHandler = gameModeHandler;
            _shipPlayer = shipPlayer;
            _onRestart = onRestart;
            _onComplete = onComplete;
        }

        public void EnableGameInput() {
            _shipPlayer.User.EnableGameInput();
        }

        public void DisableAllShipInput() {
            _shipPlayer.Freeze = true;
            _shipPlayer.User.movementEnabled = false;
            _shipPlayer.User.boostButtonForceEnabled = false;
        }

        public void EnableShipInput() {
            _shipPlayer.Freeze = false;
            _shipPlayer.User.movementEnabled = true;
            _shipPlayer.User.ResetMouseToCentre();
        }

        public void EnableShipBoostInput() {
            _shipPlayer.User.boostButtonForceEnabled = true;
        }

        public void Restart() {
            _onRestart();
        }

        public void Complete() {
            _onComplete();
        }
    }
}