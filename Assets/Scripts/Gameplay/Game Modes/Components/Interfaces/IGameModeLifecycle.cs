namespace Gameplay.Game_Modes.Components.Interfaces {
    public interface IGameModeLifecycle {
        public bool HasStarted { get; }
        public void Restart();
        public void Complete();
    }
}