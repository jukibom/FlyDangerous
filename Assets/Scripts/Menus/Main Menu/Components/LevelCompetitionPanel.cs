using System.Collections.Generic;
using System.Linq;
using Core;
using Core.MapData;
using Core.Replays;
using UnityEngine;

namespace Menus.Main_Menu.Components {
    public class LevelCompetitionPanel : MonoBehaviour {
        [SerializeField] private GhostList ghostList;
        [SerializeField] private Leaderboard leaderboard;

        public void PopulateGhostsForLevel(Level level) {
            ghostList.PopulateGhostsForLevel(level);
        }

        public async void PopulateLeaderboardForLevel(Level level) {
            leaderboard.gameObject.SetActive(FdNetworkManager.Instance.HasLeaderboardServices);
            if (FdNetworkManager.Instance.HasLeaderboardServices) {
                var leaderboardData = await FdNetworkManager.Instance.OnlineService!.Leaderboard!.FindOrCreateLeaderboard(level.Data.LevelHash());
                leaderboard.LoadLeaderboard(leaderboardData);
            }
        }

        public List<Replay> GetSelectedReplays() {
            return ghostList.GetComponentsInChildren<GhostEntry>().ToList().FindAll(entry => entry.isEnabled.isChecked).ConvertAll(entry => entry.replay);
        }
    }
}