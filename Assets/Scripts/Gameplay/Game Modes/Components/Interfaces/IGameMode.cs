using GameUI.GameModes;

namespace Gameplay.Game_Modes.Components.Interfaces {
    public interface IGameMode {
        // Initialisation
        public GameModeUIHandler GameModeUIHandler { set; }
        public IGameModeLifecycle GameModeLifecycle { set; }

        /**
         * Flag set to prompt game to record a replay for this game mode
         */
        public bool SupportsReplays { get; }

        /**
         * Prevent ship movement unless the boost button is held.
         * This check is handled at the start of the lifecycle, before input is enabled.
         */
        public bool RequireBoostHeldToStart { get; }

        /**
         * Called at the start and any time the user or game mechanics cause a restart
         */
        public void OnBegin();

        /**
         * Called with standard unity FixedUpdate
         */
        public void OnFixedUpdate();

        /**
         * Called when the user hits restart, the state of the game mode must be reset.
         */
        public void OnRestart();

        /**
         * Called on game mode completion (e.g. hit the last checkpoint, timer ran out etc)
         */
        public void OnComplete();

        /**
         * Called if the user (or game host) exits the game mode
         */
        public void OnQuit();

        /**
         * Called when the user changes game preferences
         */
        public void OnGameSettingsApplied();
    }
}