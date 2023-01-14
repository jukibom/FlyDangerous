using Gameplay.Game_Modes.Components.Interfaces;
using GameUI.GameModes;

namespace Gameplay.Game_Modes {
    public class HoonAttack : IGameMode {
        public GameModeUIHandler GameModeUIHandler { get; set; }
        public IGameModeLifecycle GameModeLifecycle { get; set; }

        public bool IsHotJoinable => false;
        public bool CanWarpToHost => false;
        public bool HasFixedStartLocation => false;
        public bool SupportsReplays => true;
        public bool RequireBoostHeldToStart => true;

        public void OnInitialise() {
        }

        public void OnBegin() {
        }

        public void OnFixedUpdate() {
        }

        public void OnRestart() {
        }

        public void OnComplete() {
        }

        public void OnQuit() {
        }

        public void OnGameSettingsApplied() {
        }
    }
}