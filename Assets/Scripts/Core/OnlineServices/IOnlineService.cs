using JetBrains.Annotations;

namespace Core.OnlineServices {
    public interface IOnlineService {
        public string OnlineServiceName { get; }
        [CanBeNull] public string PlayerName { get; }
        [CanBeNull] public ILeaderboardService Leaderboard { get; }
        [CanBeNull] public IMultiplayerService Multiplayer { get; }
    }
}