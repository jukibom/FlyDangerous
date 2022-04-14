#if !DISABLESTEAMWORKS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Player;
using Steamworks;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamLeaderboard : ILeaderboard {
        private readonly CallResult<LeaderboardScoresDownloaded_t> _entriesFetchResultsCallback;
        private readonly SteamLeaderboard_t _leaderboard;
        private readonly CallResult<LeaderboardScoreUploaded_t> _uploadResultCallback;

        private TaskCompletionSource<List<ILeaderboardEntry>> _leaderboardEntryListTask;
        private TaskCompletionSource<bool> _uploadScoreTask;

        public SteamLeaderboard(SteamLeaderboard_t leaderboard) {
            _leaderboard = leaderboard;
            _entriesFetchResultsCallback = CallResult<LeaderboardScoresDownloaded_t>.Create(OnEntriesRetrieved);
            _uploadResultCallback = CallResult<LeaderboardScoreUploaded_t>.Create(OnUpload);
        }

        public Task<List<ILeaderboardEntry>> GetEntries(LeaderboardFetchType fetchType) {
            TaskHandler.RecreateTask(ref _leaderboardEntryListTask);

            var entriesRequest = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal;
            switch (fetchType) {
                case LeaderboardFetchType.Top:
                    entriesRequest = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal;
                    break;
                case LeaderboardFetchType.Me:
                    entriesRequest = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser;
                    break;
                case LeaderboardFetchType.Friends:
                    entriesRequest = ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends;
                    break;
            }

            var handle = SteamUserStats.DownloadLeaderboardEntries(_leaderboard, entriesRequest, 0, 20);
            _entriesFetchResultsCallback.Set(handle);

            return _leaderboardEntryListTask.Task;
        }

        public Task UploadScore(int score, Flag flag) {
            TaskHandler.RecreateTask(ref _uploadScoreTask);

            var details = SteamLeaderboardEntry.GetEntryDetails(flag);
            var method = ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest;
            var handle = SteamUserStats.UploadLeaderboardScore(_leaderboard, method, score, details, SteamLeaderboardEntry.SteamDetailsCount);
            _uploadResultCallback.Set(handle);

            return _uploadScoreTask.Task;
        }

        ~SteamLeaderboard() {
            _entriesFetchResultsCallback.Dispose();
            _uploadResultCallback.Dispose();
        }

        private void OnEntriesRetrieved(LeaderboardScoresDownloaded_t ctx, bool ioFailure) {
            List<ILeaderboardEntry> entries = new();
            for (var i = 0; i < ctx.m_cEntryCount; i++) {
                var details = new int[SteamLeaderboardEntry.SteamDetailsCount];
                SteamUserStats.GetDownloadedLeaderboardEntry(ctx.m_hSteamLeaderboardEntries, i, out var leaderboardEntry, details,
                    SteamLeaderboardEntry.SteamDetailsCount);

                entries.Add(new SteamLeaderboardEntry(leaderboardEntry, details));
            }

            _leaderboardEntryListTask.SetResult(entries);
        }

        private void OnUpload(LeaderboardScoreUploaded_t ctx, bool ioFailure) {
            if (ctx.m_bSuccess == 1 && !ioFailure) _uploadScoreTask.SetResult(true);
            else _uploadScoreTask.SetException(new Exception("Failed to upload score"));
        }
    }
}
#endif