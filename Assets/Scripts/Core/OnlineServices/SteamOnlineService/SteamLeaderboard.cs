#if !DISABLESTEAMWORKS
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Steamworks;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamLeaderboard : ILeaderboard {
        private readonly CallResult<LeaderboardUGCSet_t> _attachGhostToEntryCallback;
        private readonly CallResult<LeaderboardScoresDownloaded_t> _entriesFetchResultsCallback;
        private readonly SteamLeaderboard_t _leaderboard;
        private readonly CallResult<RemoteStorageFileShareResult_t> _shareGhostCallback;
        private readonly CallResult<RemoteStorageFileWriteAsyncComplete_t> _uploadGhostCallback;
        private readonly CallResult<LeaderboardScoreUploaded_t> _uploadResultCallback;

        private TaskCompletionSource<List<ILeaderboardEntry>> _leaderboardEntryListTask;
        [CanBeNull] private string _pendingReplayUploadFileName;
        [CanBeNull] private string _pendingReplayUploadFilePath;
        private TaskCompletionSource<bool> _uploadScoreTask;

        public SteamLeaderboard(SteamLeaderboard_t leaderboard) {
            _leaderboard = leaderboard;
            _entriesFetchResultsCallback = CallResult<LeaderboardScoresDownloaded_t>.Create(OnEntriesRetrieved);
            _uploadResultCallback = CallResult<LeaderboardScoreUploaded_t>.Create(OnLeaderboardUpload);
            _uploadGhostCallback = CallResult<RemoteStorageFileWriteAsyncComplete_t>.Create(OnGhostUpload);
            _shareGhostCallback = CallResult<RemoteStorageFileShareResult_t>.Create(OnGhostShared);
            _attachGhostToEntryCallback = CallResult<LeaderboardUGCSet_t>.Create(OnGhostAttachedToEntry);
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

            var handle = SteamUserStats.DownloadLeaderboardEntries(_leaderboard, entriesRequest, -6, 43);
            _entriesFetchResultsCallback.Set(handle);

            return _leaderboardEntryListTask.Task;
        }

        public Task UploadScore(int score, int flagId, string replayFilePath, string replayFileName) {
            TaskHandler.RecreateTask(ref _uploadScoreTask);

            _pendingReplayUploadFilePath = replayFilePath;
            _pendingReplayUploadFileName = replayFileName;

            var details = SteamLeaderboardEntry.GetEntryDetails(flagId);
            var method = ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest;
            var handle = SteamUserStats.UploadLeaderboardScore(_leaderboard, method, score, details, SteamLeaderboardEntry.SteamDetailsCount);
            _uploadResultCallback.Set(handle);
            return _uploadScoreTask.Task;
        }

        ~SteamLeaderboard() {
            _entriesFetchResultsCallback.Dispose();
            _uploadResultCallback.Dispose();
            _uploadGhostCallback.Dispose();
            _shareGhostCallback.Dispose();
            _attachGhostToEntryCallback.Dispose();
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

        // On upload we need to successively upload and then attach the ghost files before resolving the task.
        private void OnLeaderboardUpload(LeaderboardScoreUploaded_t ctx, bool ioFailure) {
            if (ctx.m_bSuccess == 1 && !ioFailure && _pendingReplayUploadFilePath != null && _pendingReplayUploadFileName != null) {
                // if new high score on the leaderboard, update the ghost
                if (Convert.ToBoolean(ctx.m_bScoreChanged)) {
                    var replay = File.ReadAllBytes(_pendingReplayUploadFilePath);
                    var handle = SteamRemoteStorage.FileWriteAsync(_pendingReplayUploadFileName, replay, Convert.ToUInt32(replay.Length));
                    _uploadGhostCallback.Set(handle);
                }
                else {
                    _uploadScoreTask.SetResult(true);
                }
            }
            else {
                _uploadScoreTask.SetException(new Exception("Failed to upload score"));
            }
        }

        private void OnGhostUpload(RemoteStorageFileWriteAsyncComplete_t ctx, bool ioFailure) {
            if (ctx.m_eResult == EResult.k_EResultOK && !ioFailure) {
                var handle = SteamRemoteStorage.FileShare(_pendingReplayUploadFileName);
                _shareGhostCallback.Set(handle);
            }
            else {
                _uploadScoreTask.SetException(new Exception("Failed to upload score"));
            }
        }

        private void OnGhostShared(RemoteStorageFileShareResult_t ctx, bool ioFailure) {
            if (ctx.m_eResult == EResult.k_EResultOK && !ioFailure) {
                var handle = SteamUserStats.AttachLeaderboardUGC(_leaderboard, ctx.m_hFile);
                _attachGhostToEntryCallback.Set(handle);
            }
            else {
                _uploadScoreTask.SetException(new Exception("Failed to upload ghost " + ctx.m_eResult));
            }
        }

        private void OnGhostAttachedToEntry(LeaderboardUGCSet_t ctx, bool ioFailure) {
            if (ctx.m_eResult == EResult.k_EResultOK && !ioFailure)
                _uploadScoreTask.SetResult(true);
            else
                _uploadScoreTask.SetException(new Exception("Failed to attach replay to leaderboard entry"));
        }
    }
}
#endif