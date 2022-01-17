
using JetBrains.Annotations;

namespace Core.OnlineServices {

    public interface IOnlineService {
        [CanBeNull] public ILeaderboardService Leaderboard { get; }
        [CanBeNull] public IMultiplayerService Multiplayer { get; }
    }
}