#if !DISABLESTEAMWORKS

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamOnlineService : IOnlineService {
        private readonly SteamMultiplayer _multiplayer = new SteamMultiplayer();
        private readonly SteamLeaderboard _leaderboard = new SteamLeaderboard();
        
        public IMultiplayerService Multiplayer => _multiplayer;
        public ILeaderboardService Leaderboard => _leaderboard;
    }
}

#endif // !DISABLESTEAMWORKS