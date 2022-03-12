using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.OnlineServices {
    public struct LobbyInfo {
        public string lobbyId;
        public string connectionAddress;
        public string name;
        public int players;
        public int playersMax;
        public string gameMode;
    }

    public interface IMultiplayerService {
        public Task CreateLobby();
        public Task JoinLobby(string lobbyAddress);
        public Task<List<string>> GetLobbyList();
        public Task<LobbyInfo> GetLobbyInfo(string lobbyId);
    }
}