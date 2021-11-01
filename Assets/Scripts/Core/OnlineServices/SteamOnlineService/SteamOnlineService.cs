#if !DISABLESTEAMWORKS
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Steamworks;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamOnlineService : IOnlineService {

        private readonly SteamLobby _steamLobby = new SteamLobby();
        private readonly SteamHooks _steamHooks;

        public SteamOnlineService() {
            _steamHooks = new SteamHooks(OnJoinGameRequest);
        }
        
        public Task CreateLobby() {
            return _steamLobby.CreateLobby();
        }
        
        public Task JoinLobby(string lobbyAddress) {
            // convert our string into a CSteamID
            var steamId = ulong.Parse(lobbyAddress);
            return _steamLobby.JoinLobby(new CSteamID(steamId));
        }

        public async Task<List<string>> GetLobbyList() {
            var lobbies = await _steamLobby.GetLobbyList();
            return lobbies.Select(cSteamID => cSteamID.m_SteamID.ToString()).ToList();
        }

        public Task<LobbyInfo> GetLobbyInfo(string lobbyId) {
            return _steamLobby.GetLobbyInfo(new CSteamID(ulong.Parse(lobbyId)));
        }

        private void OnJoinGameRequest(string address) {
            // TODO: Handle join request from any context (oh god state stuff why)
        }
    }
}
#endif // !DISABLESTEAMWORKS