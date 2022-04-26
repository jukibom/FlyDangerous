using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core;
using Core.MapData;
using Core.Replays;
using UnityEngine;

namespace Menus.Main_Menu.Components {
    public class LevelCompetitionPanel : MonoBehaviour {
        [SerializeField] private GhostList ghostList;
        [SerializeField] private Leaderboard leaderboard;

        private LevelData _levelData;

        public void Populate(LevelData levelData) {
            _levelData = levelData;
            PopulateLeaderboardForLevel();
            PopulateGhostsForLevel();
        }

        public List<Replay> GetSelectedReplays() {
            return ghostList.GetComponentsInChildren<GhostEntry>().ToList().FindAll(entry => entry.checkbox.isChecked).ConvertAll(entry => entry.replay);
        }

        public async void DownloadGhost(LeaderboardEntry leaderboardEntry) {
            var currentSelectedReplays = GetSelectedReplays();
            var directory = Path.Combine(Replay.ReplayDirectory, _levelData.LevelHash());
            var filePath = await leaderboardEntry.DownloadReplay(directory);
            if (filePath != "") {
                var newReplay = Replay.LoadFromFilepath(filePath);
                PopulateGhostsForLevel();

                SelectReplaysInList(currentSelectedReplays);
                SelectReplayInList(newReplay);
            }
        }

        public void DeleteGhost(GhostEntry ghostEntry) {
            ghostEntry.replay.Delete();
            PopulateGhostsForLevel();
        }

        private void PopulateGhostsForLevel() {
            ghostList.PopulateGhostsForLevel(_levelData);
            if (Game.Instance.ActiveGameReplays != null)
                SelectReplaysInList(Game.Instance.ActiveGameReplays);
        }

        private async void PopulateLeaderboardForLevel() {
            leaderboard.gameObject.SetActive(FdNetworkManager.Instance.HasLeaderboardServices);
            if (FdNetworkManager.Instance.HasLeaderboardServices) {
                var leaderboardData = await FdNetworkManager.Instance.OnlineService!.Leaderboard!.FindOrCreateLeaderboard(_levelData.LevelHash());
                leaderboard.LoadLeaderboard(leaderboardData);
            }
        }

        private void SelectReplaysInList(List<Replay> replays) {
            ghostList.GetComponentsInChildren<GhostEntry>().ToList().ForEach(ghost => {
                if (replays.Exists(replay => replay.Hash == ghost.replay.Hash))
                    ghost.checkbox.isChecked = true;
            });
        }

        private void SelectReplayInList(Replay replay) {
            ghostList.GetComponentsInChildren<GhostEntry>().ToList().ForEach(ghost => {
                if (ghost.replay.Hash == replay.Hash) ghost.checkbox.isChecked = true;
            });
        }
    }
}