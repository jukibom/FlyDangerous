using GameUI.GameModes;

namespace Gameplay.Game_Modes.Components.Interfaces {
    public interface IGameMode {
        // Initialisation
        public GameModeUIHandler GameModeUIHandler { set; }
        public IGameModeLifecycle GameModeLifecycle { set; }

        /**
         * Can users hot-join a session in multiplayer and bypass the lobby?
         */
        public bool IsHotJoinable { get; }

        /**
         * Can clients warp to the host with reset in multiplayer?
         */
        public bool CanWarpToHost { get; }

        /**
         * Does this game mode have a fixed start location (vs. dynamic / random)?
         */
        public bool HasFixedStartLocation { get; }

        /**
         * denotes that a restart can always immediately snap without any load time (e.g. if a start position keeps terrain in memory)
         */
        public bool IsStartLocationAlwaysPreLoaded { get; }

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
         * Called during level load, before the game fades in from black in case the game
         * mode needs to do any additional work before the level is visible.
         */
        public void OnInitialise();

        /**
         * Called at the start, once the level has fully loaded and the screen has faded in.
         */
        public void OnBegin();

        /**
         * Called with standard unity FixedUpdate.
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