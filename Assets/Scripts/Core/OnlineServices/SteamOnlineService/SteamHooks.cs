#if !DISABLESTEAMWORKS
using System;
using Steamworks;
using UnityEngine;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamHooks {
        private readonly Callback<GameLobbyJoinRequested_t> _lobbyJoinRequestedCallback;
        private Action<string> OnLobbyJoin;
        
        public SteamHooks(Action<string> onLobbyJoin) {
            OnLobbyJoin = onLobbyJoin;
            // join request from friend list
            _lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        }

        ~SteamHooks() {
            _lobbyJoinRequestedCallback.Dispose();
        }
        
        private void OnLobbyJoinRequested(GameLobbyJoinRequested_t param) {
            Debug.Log("lobby " + param.m_steamIDLobby); // use with GetLobbyInfo
            Debug.Log("user " + param.m_steamIDFriend); // use to connect directly
            OnLobbyJoin(param.m_steamIDFriend.ToString());
        }
    }
}
#endif // !DISABLESTEAMWORKS