#if !DISABLESTEAMWORKS
namespace Core.OnlineServices.SteamOnlineService {
    public class SteamOnlineService : IOnlineService {
        private readonly SteamLeaderboardService _leaderboardService = new();
        private readonly SteamMultiplayer _multiplayer = new();

        public IMultiplayerService Multiplayer => _multiplayer;
        public ILeaderboardService Leaderboard => _leaderboardService;
    }
}

#endif // !DISABLESTEAMWORKS