using System;
using Gameplay.Game_Modes.Components.Interfaces;
using GameUI.GameModes;

namespace Gameplay.Game_Modes {
    public class HoonAttack : IGameMode {
        public GameModeUIHandler GameModeUIHandler { get; set; }
        public IGameModeLifecycle GameModeLifecycle { get; set; }
        public bool SupportsReplays => true;
        public bool RequireBoostHeldToStart => true;

        public void OnInitialise() {
            throw new NotImplementedException();
        }

        public void OnBegin() {
            throw new NotImplementedException();
        }

        public void OnFixedUpdate() {
            throw new NotImplementedException();
        }

        public void OnRestart() {
            throw new NotImplementedException();
        }

        public void OnComplete() {
            throw new NotImplementedException();
        }

        public void OnQuit() {
            throw new NotImplementedException();
        }

        public void OnGameSettingsApplied() {
            throw new NotImplementedException();
        }
    }
}