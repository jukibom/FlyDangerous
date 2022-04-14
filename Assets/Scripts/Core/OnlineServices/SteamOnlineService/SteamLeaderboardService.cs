#if !DISABLESTEAMWORKS
using System;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamLeaderboardService : ILeaderboardService {
        private readonly CallResult<LeaderboardFindResult_t> _leaderboardFetchCallback;

        private TaskCompletionSource<ILeaderboard> _leaderboardFetchTask;

        public SteamLeaderboardService() {
            if (!SteamManager.Initialized) {
                Debug.LogWarning("Steam Manager not initialised.");
                return;
            }

            _leaderboardFetchCallback = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFetch);
        }


        public Task<ILeaderboard> FindOrCreateLeaderboard(string id) {
            TaskHandler.RecreateTask(ref _leaderboardFetchTask);
            var handle = SteamUserStats.FindOrCreateLeaderboard(id, ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending,
                ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeMilliSeconds);
            _leaderboardFetchCallback.Set(handle);
            return _leaderboardFetchTask.Task;
        }

        ~SteamLeaderboardService() {
            _leaderboardFetchCallback.Dispose();
        }

        private void OnLeaderboardFetch(LeaderboardFindResult_t ctx, bool ioFailure) {
            if (ioFailure || ctx.m_bLeaderboardFound == 0)
                _leaderboardFetchTask.SetException(new Exception("Failed to fetch leaderboard"));
            else
                _leaderboardFetchTask.SetResult(new SteamLeaderboard(ctx.m_hSteamLeaderboard));
        }
    }
}
#endif