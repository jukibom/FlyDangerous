using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Core.OnlineServices {
    public interface ILeaderboardEntry {
        public string Player { get; }
        public float Score { get; }

        public string FlagLocale { get; }

        public Task<IOnlineFile> Replay();
    }

    public interface ILeaderboard {
        // TODO: Handle pagination - for now let's just show the top 20 and call it a day (we need lots of entries to properly test)
        public Task<List<ILeaderboardEntry>> GetEntries();
        public Task UploadScore(int score, string flagIsoCode);
    }

    public interface ILeaderboardService {
        public Task<ILeaderboard> FindOrCreateLeaderboard(string id);
    }

    public interface IOnlineFile {
        public string Filename { get; }
        public MemoryStream Data { get; }
    }
}