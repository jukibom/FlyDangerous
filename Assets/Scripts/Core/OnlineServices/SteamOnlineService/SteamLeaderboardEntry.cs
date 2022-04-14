#if !DISABLESTEAMWORKS
using System;
using System.Threading.Tasks;
using Core.Player;
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
            Flag = Flag.FromFixedId(details[0]);

            _replayFetchCallback = CallResult<RemoteStorageDownloadUGCResult_t>.Create(OnReplayFetch);
        }

        public int Rank { get; }
        public string Player { get; }
        public int Score { get; }
        public Flag Flag { get; }

        public Task<IOnlineFile> Replay() {
            TaskHandler.RecreateTask(ref _replayFetchTask);

            SteamRemoteStorage.UGCDownload(_leaderboardEntry.m_hUGC, 1);
            return _replayFetchTask.Task;
        }

        ~SteamLeaderboardEntry() {
            _replayFetchCallback.Dispose();
        }

        private void OnReplayFetch(RemoteStorageDownloadUGCResult_t ctx, bool ioFailure) {
            if (ioFailure)
                _replayFetchTask.SetException(new Exception("Failed to fetch replay file"));
            else
                _replayFetchTask.SetResult(new SteamFileStore(ctx));
        }

        public static int[] GetEntryDetails(Flag flag) {
            var details = new int[SteamDetailsCount];
            details[0] = flag.FixedId;
            return details;
        }
    }
}
#endif