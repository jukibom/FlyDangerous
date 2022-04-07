#if !DISABLESTEAMWORKS
using System;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamLeaderboardService : ILeaderboardService {
        private readonly Callback<LeaderboardFindResult_t> _lobbyFetchCallback;

        private TaskCompletionSource<ILeaderboard> _leaderboardFetchTask;

        public SteamLeaderboardService() {
            if (!SteamManager.Initialized) {
                Debug.LogWarning("Steam Manager not initialised.");
                return;
            }

            _lobbyFetchCallback = Callback<LeaderboardFindResult_t>.Create(OnLeaderboardFetch);
        }


        public Task<ILeaderboard> FindOrCreateLeaderboard(string id) {
            TaskHandler.RecreateTask(ref _leaderboardFetchTask);
            SteamUserStats.FindOrCreateLeaderboard(id, ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending,
                ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeMilliSeconds);
            return _leaderboardFetchTask.Task;
        }

        ~SteamLeaderboardService() {
            _lobbyFetchCallback.Dispose();
        }

        private void OnLeaderboardFetch(LeaderboardFindResult_t ctx) {
            if (ctx.m_bLeaderboardFound == 0)
                _leaderboardFetchTask.SetException(new Exception("Failed to fetch leaderboard"));
            else
                _leaderboardFetchTask.SetResult(new SteamLeaderboard(ctx.m_hSteamLeaderboard));
        }
    }
}
#endif