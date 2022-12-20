namespace Gameplay.Game_Modes.Components.Interfaces {
    public interface IGameModeTimer {
        // Current time since the game mode started. Accounts for Restarts automatically.
        public float CurrentSessionTimeSeconds { get; }

        // The exact time when the game mode started using Fixed time (Time.fixedTime, time since application start at which this timer began)
        public float StartTimeSeconds { get; }
    }
}