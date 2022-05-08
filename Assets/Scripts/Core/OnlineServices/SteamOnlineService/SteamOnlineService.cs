#if !DISABLESTEAMWORKS
using Steamworks;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamOnlineService : IOnlineService {
        private readonly SteamLeaderboardService _leaderboardService = new();
        private readonly SteamMultiplayer _multiplayer = new();

        public string PlayerName => SteamManager.Initialized ? SteamFriends.GetPersonaName() : "STEAM NOT CONNECTED";
        public IMultiplayerService Multiplayer => _multiplayer;
        public ILeaderboardService Leaderboard => _leaderboardService;
    }
}

#endif // !DISABLESTEAMWORKS