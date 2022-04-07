#if !DISABLESTEAMWORKS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamLeaderboard : ILeaderboard {
        private readonly Callback<LeaderboardScoresDownloaded_t> _entriesFetchResultsCallback;
        private readonly SteamLeaderboard_t _leaderboard;
        private readonly Callback<LeaderboardScoreUploaded_t> _uploadResultCallback;

        private TaskCompletionSource<List<ILeaderboardEntry>> _leaderboardEntryListTask;
        private TaskCompletionSource<bool> _uploadScoreTask;

        public SteamLeaderboard(SteamLeaderboard_t leaderboard) {
            _leaderboard = leaderboard;
            _entriesFetchResultsCallback = Callback<LeaderboardScoresDownloaded_t>.Create(OnEntriesRetrieved);
            _uploadResultCallback = Callback<LeaderboardScoreUploaded_t>.Create(OnUpload);
        }

        public Task<List<ILeaderboardEntry>> GetEntries() {
            SteamUserStats.DownloadLeaderboardEntries(_leaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 0, 20);
            return _leaderboardEntryListTask.Task;
        }

        public Task UploadScore(int score, string flagIsoCode) {
            var details = SteamLeaderboardEntry.GetEntryDetails(flagIsoCode);
            SteamUserStats.UploadLeaderboardScore(_leaderboard, ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, details,
                SteamLeaderboardEntry.SteamDetailsCount);
            return _uploadScoreTask.Task;
        }

        ~SteamLeaderboard() {
            _entriesFetchResultsCallback.Dispose();
            _uploadResultCallback.Dispose();
        }

        private void OnEntriesRetrieved(LeaderboardScoresDownloaded_t ctx) {
            List<ILeaderboardEntry> entries = new();
            for (var i = 0; i < ctx.m_cEntryCount; i++) {
                var details = new int[SteamLeaderboardEntry.SteamDetailsCount];
                SteamUserStats.GetDownloadedLeaderboardEntry(ctx.m_hSteamLeaderboardEntries, i, out var leaderboardEntry, details,
                    SteamLeaderboardEntry.SteamDetailsCount);

                entries.Add(new SteamLeaderboardEntry(leaderboardEntry, details));
            }

            _leaderboardEntryListTask.SetResult(entries);
        }

        private void OnUpload(LeaderboardScoreUploaded_t ctx) {
            if (ctx.m_bSuccess == 1) _uploadScoreTask.SetResult(true);
            else _uploadScoreTask.SetException(new Exception("Failed to upload score"));
        }
    }
}
#endif