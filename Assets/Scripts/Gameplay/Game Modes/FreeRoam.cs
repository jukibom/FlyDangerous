using Gameplay.Game_Modes.Components.Interfaces;
using GameUI.GameModes;

namespace Gameplay.Game_Modes {
    // Not a great deal to do in Free Roam!
    // Multiplayer stuff should probably go in here eventually though
    public class FreeRoam : IGameMode {
        public GameModeUIHandler GameModeUIHandler { get; set; }
        public IGameModeLifecycle GameModeLifecycle { get; set; }

        public bool IsHotJoinable => true;
        public bool CanWarpToHost => true;
        public bool HasFixedStartLocation => false;
        public bool SupportsReplays => false;
        public bool RequireBoostHeldToStart => false;

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