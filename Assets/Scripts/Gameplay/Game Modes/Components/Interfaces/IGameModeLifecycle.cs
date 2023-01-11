namespace Gameplay.Game_Modes.Components.Interfaces {
    public interface IGameModeLifecycle {
        /**
         * Has the ship started the engines / user held boost / etc
         */
        public bool IsShipActive { get; }

        /**
         * Has the countdown completed and the game started
         */
        public bool HasStarted { get; }

        /**
         * Call a restart, notifying IGameMode.OnRestart and reinitialising the game mode handler
         */
        public void Restart();

        /**
         * Finish the game, notifying IGameMode.OnComplete and triggering end state UI etc
         */
        public void Complete();
    }
}