#if !DISABLESTEAMWORKS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace Core.OnlineServices.SteamOnlineService {
    public class SteamLobby {
        // Arbitrary data can be set in the lobby instance to be retrieved later by key reference
        private const string HostAddressVar = "HostAddress";
        private readonly Dictionary<ulong, TaskCompletionSource<LobbyInfo>> _getLobbyInfoTaskList = new();

        private readonly Callback<LobbyCreated_t> _lobbyCreatedCallback;
        private readonly Callback<LobbyEnter_t> _lobbyEnteredCallback;
        private readonly Callback<LobbyDataUpdate_t> _lobbyInfoCallback;
        private readonly Callback<LobbyMatchList_t> _lobbyListRequestedCallback;

        private TaskCompletionSource<bool> _createLobbyTask;

        private ulong _currentSteamLobbyID;
        private TaskCompletionSource<List<CSteamID>> _getLobbyListTask;
        private TaskCompletionSource<bool> _joinLobbyTask;

        public SteamLobby() {
            if (!SteamManager.Initialized) {
                Debug.LogWarning("Steam Manager not initialised.");
                return;
            }

            _lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyJoined);
            _lobbyListRequestedCallback = Callback<LobbyMatchList_t>.Create(OnGetLobbiesList);
            _lobbyInfoCallback = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyInfo);
        }

        ~SteamLobby() {
            _lobbyCreatedCallback.Dispose();
            _lobbyEnteredCallback.Dispose();
            _lobbyListRequestedCallback.Dispose();
            _lobbyInfoCallback.Dispose();
        }

        public Task CreateLobby() {
            TaskHandler.RecreateTask(ref _createLobbyTask);
            // TODO: UI for this! D: fuck fuck fuck
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 16);
            return _createLobbyTask.Task;
        }

        public Task<bool> JoinLobby(CSteamID lobbyId) {
            TaskHandler.RecreateTask(ref _joinLobbyTask);
            SteamMatchmaking.JoinLobby(lobbyId);
            return _joinLobbyTask.Task;
        }

        public Task<List<CSteamID>> GetLobbyList() {
            TaskHandler.RecreateTask(ref _getLobbyListTask);
            SteamMatchmaking.RequestLobbyList();
            return _getLobbyListTask.Task;
        }

        public Task<LobbyInfo> GetLobbyInfo(CSteamID lobbyId) {
            var taskSource = new TaskCompletionSource<LobbyInfo>();
            _getLobbyInfoTaskList.TryAdd(lobbyId.m_SteamID, taskSource);
            SteamMatchmaking.RequestLobbyData(lobbyId);
            return taskSource.Task;
        }

        private void OnLobbyCreated(LobbyCreated_t ctx) {
            Debug.Log("OnLobbyCreated " + SteamUser.GetSteamID());
            if (ctx.m_eResult != EResult.k_EResultOK) {
                _createLobbyTask.SetException(new Exception("Failed to create Steam lobby."));
                return;
            }

            Debug.Log("Setting lobby address " + SteamUser.GetSteamID());
            var steamId = new CSteamID(ctx.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyData(steamId, HostAddressVar, SteamUser.GetSteamID().ToString());
            SteamMatchmaking.SetLobbyData(steamId, "name", SteamFriends.GetPersonaName() + "'s Game");
            SteamMatchmaking.SetLobbyData(steamId, "gameType", "Free Roam");

            // TODO: Unset this on lobby destroy (oof)
            // TODO: Control this better generally

            SteamFriends.SetRichPresence("status", "In Multiplayer Lobby");
            SteamFriends.SetRichPresence("connect", $"connect/{SteamUser.GetSteamID().ToString()}");

            _createLobbyTask.SetResult(true);
        }

        private void OnLobbyJoined(LobbyEnter_t ctx) {
            if (_joinLobbyTask != null) {
                _currentSteamLobbyID = ctx.m_ulSteamIDLobby;
                Debug.Log("OnLobbyEntered for lobby with id: " + _currentSteamLobbyID);

                _joinLobbyTask.SetResult(true);
            }
        }

        private void OnGetLobbiesList(LobbyMatchList_t ctx) {
            var lobbies = new List<CSteamID>();
            for (var i = 0; i < ctx.m_nLobbiesMatching; i++) lobbies.Add(SteamMatchmaking.GetLobbyByIndex(i));
            _getLobbyListTask.SetResult(lobbies);
        }

        private void OnGetLobbyInfo(LobbyDataUpdate_t ctx) {
            var hasCallback = _getLobbyInfoTaskList.TryGetValue(ctx.m_ulSteamIDLobby, out var task);
            if (hasCallback) {
                var lobbyId = new CSteamID(ctx.m_ulSteamIDLobby);
                var lobbyInfo = new LobbyInfo {
                    lobbyId = ctx.m_ulSteamIDLobby.ToString(),
                    connectionAddress = SteamMatchmaking.GetLobbyData(lobbyId, HostAddressVar),
                    players = SteamMatchmaking.GetNumLobbyMembers(lobbyId),
                    playersMax = SteamMatchmaking.GetLobbyMemberLimit(lobbyId),
                    name = SteamMatchmaking.GetLobbyData(lobbyId, "name"),
                    gameMode = SteamMatchmaking.GetLobbyData(lobbyId, "gameType")
                };
                _getLobbyInfoTaskList.Remove(ctx.m_ulSteamIDLobby);
                task.SetResult(lobbyInfo);
            }
        }
    }
}
#endif // !DISABLESTEAMWORKS