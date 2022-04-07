#if !DISABLESTEAMWORKS
using System;
using System.Threading.Tasks;
using Steamworks;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamLeaderboardEntry : ILeaderboardEntry {
        
        public static readonly int SteamDetailsCount = 1;

        private readonly Callback<RemoteStorageDownloadUGCResult_t> _replayFetchCallback;
        private readonly LeaderboardEntry_t _leaderboardEntry;

        private TaskCompletionSource<IOnlineFile> _replayFetchTask;

        public string Player { get; }
        public float Score { get; }
        public string FlagLocale { get; }

        
        public SteamLeaderboardEntry(LeaderboardEntry_t leaderboardEntry, Int32[] details) {
            _leaderboardEntry = leaderboardEntry;

            Player = SteamFriends.GetFriendPersonaName(_leaderboardEntry.m_steamIDUser);
            Score = _leaderboardEntry.m_nScore;
            FlagLocale = Convert.ToString(details[0]);

            _replayFetchCallback = Callback<RemoteStorageDownloadUGCResult_t>.Create(OnReplayFetch);
        }

        ~SteamLeaderboardEntry() {
            _replayFetchCallback.Dispose();
        }
        
        public Task<IOnlineFile> Replay() {
            SteamRemoteStorage.UGCDownload(_leaderboardEntry.m_hUGC, 1);
            return _replayFetchTask.Task;
        }

        private void OnReplayFetch(RemoteStorageDownloadUGCResult_t ctx) {
            _replayFetchTask.SetResult(new SteamFileStore(ctx));
        }

        public static int[] GetEntryDetails(string flagIsoString) {
            var details = new int[SteamDetailsCount];
            details[0] = Convert.ToInt32(flagIsoString);
            return details;
        }
    }
}
#endif