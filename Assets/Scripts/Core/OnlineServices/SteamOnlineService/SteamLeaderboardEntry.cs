#if !DISABLESTEAMWORKS
using System;
using System.Threading.Tasks;
using Steamworks;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamLeaderboardEntry : ILeaderboardEntry {
        public static readonly int SteamDetailsCount = 1;
        private readonly LeaderboardEntry_t _leaderboardEntry;

        private readonly CallResult<RemoteStorageDownloadUGCResult_t> _replayFetchCallback;

        private TaskCompletionSource<IOnlineFile> _replayFetchTask;


        public SteamLeaderboardEntry(LeaderboardEntry_t leaderboardEntry, int[] details) {
            _leaderboardEntry = leaderboardEntry;

            Rank = _leaderboardEntry.m_nGlobalRank;
            Player = SteamFriends.GetFriendPersonaName(_leaderboardEntry.m_steamIDUser);
            Score = _leaderboardEntry.m_nScore;
            FlagId = details[0];

            _replayFetchCallback = CallResult<RemoteStorageDownloadUGCResult_t>.Create(OnReplayFetch);
        }

        public int Rank { get; }
        public string Player { get; }
        public int Score { get; }
        public int FlagId { get; }

        public Task<IOnlineFile> Replay() {
            TaskHandler.RecreateTask(ref _replayFetchTask);

            var handle = SteamRemoteStorage.UGCDownload(_leaderboardEntry.m_hUGC, 1);
            _replayFetchCallback.Set(handle);
            return _replayFetchTask.Task;
        }

        ~SteamLeaderboardEntry() {
            _replayFetchCallback.Dispose();
        }

        private void OnReplayFetch(RemoteStorageDownloadUGCResult_t ctx, bool ioFailure) {
            if (ctx.m_eResult == EResult.k_EResultOK && !ioFailure)
                _replayFetchTask.SetResult(new SteamFileStore(ctx));
            else
                _replayFetchTask.SetException(new Exception("Failed to fetch replay file " + ctx.m_eResult));
        }

        public static int[] GetEntryDetails(int flagId) {
            var details = new int[SteamDetailsCount];
            details[0] = flagId;
            return details;
        }
    }
}
#endif