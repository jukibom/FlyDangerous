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
            PopulateGhostsForLevel();
            PopulateLeaderboardForLevel();
        }

        private void PopulateGhostsForLevel() {
            ghostList.PopulateGhostsForLevel(_levelData);
        }

        private async void PopulateLeaderboardForLevel() {
            leaderboard.gameObject.SetActive(FdNetworkManager.Instance.HasLeaderboardServices);
            if (FdNetworkManager.Instance.HasLeaderboardServices) {
                var leaderboardData = await FdNetworkManager.Instance.OnlineService!.Leaderboard!.FindOrCreateLeaderboard(_levelData.LevelHash());
                leaderboard.LoadLeaderboard(leaderboardData);
            }
        }

        public List<Replay> GetSelectedReplays() {
            return ghostList.GetComponentsInChildren<GhostEntry>().ToList().FindAll(entry => entry.isEnabled.isChecked).ConvertAll(entry => entry.replay);
        }

        public async void DownloadGhost(LeaderboardEntry leaderboardEntry) {
            var path = Path.Combine(Replay.ReplayDirectory, _levelData.LevelHash());
            await leaderboardEntry.DownloadReplay(path);
            PopulateGhostsForLevel();
            // TODO: set the ghost as enabled if possible
        }
    }
}