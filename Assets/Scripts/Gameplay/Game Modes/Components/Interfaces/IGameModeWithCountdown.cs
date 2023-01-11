namespace Gameplay.Game_Modes.Components.Interfaces {
    /**
     * IGameMode classes implementing this object will force the game to prevent all input to be prevented
     * until the StartingCountdownTime is complete. On every second a beep sound will play.
     * If AllowEarlyBoost is enabled, the player may boost 1 second before the countdown completes.
     * If RequireBoostHeldToStart is enabled, the game will not begin countdown until boost is held.
     * Once countdown has completed, the CountdownComplete will fire, notifying the object.
     */
    public interface IGameModeWithCountdown {
        public bool AllowEarlyBoost { get; }
        public float StartingCountdownTime { get; }
        public void CountdownStarted();
        public void CountdownComplete();
    }
}