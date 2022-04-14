using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Core.OnlineServices {
    public enum LeaderboardFetchType {
        Top,
        Me,
        Friends
    }

    public interface ILeaderboardEntry {
        public int Rank { get; }
        public string Player { get; }
        public int Score { get; }
        public int FlagId { get; }
        public Task<IOnlineFile> Replay();
    }

    public interface ILeaderboard {
        // TODO: Handle pagination - for now let's just show the top 20 and call it a day (we need lots of entries to properly test)
        public Task<List<ILeaderboardEntry>> GetEntries(LeaderboardFetchType fetchType);
        public Task UploadScore(int score, int flagId, string replayFilePath, string replayFileName);
    }

    public interface ILeaderboardService {
        public Task<ILeaderboard> FindOrCreateLeaderboard(string id);
    }

    public interface IOnlineFile {
        public string Filename { get; }
        public MemoryStream Data { get; }
    }
}