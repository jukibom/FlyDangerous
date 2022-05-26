using Core.OnlineServices;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class Leaderboard : MonoBehaviour {
        [SerializeField] private RectTransform container;
        [SerializeField] private LeaderboardEntry leaderboardEntryPrefab;
        [SerializeField] private Text leaderboardText;

        [CanBeNull] private ILeaderboard _leaderboard;

        public void LoadLeaderboard(ILeaderboard leaderboard) {
            _leaderboard = leaderboard;
            ClearEntries();
            ShowMe();
        }

        private void ClearEntries() {
            var entries = container.gameObject.GetComponentsInChildren<LeaderboardEntry>();
            foreach (var leaderboardEntry in entries) Destroy(leaderboardEntry.gameObject);
        }

        public void ShowTop20() {
            ClearEntries();
            GetEntries(LeaderboardFetchType.Top);
        }

        public void ShowMe() {
            ClearEntries();
            GetEntries(LeaderboardFetchType.Me);
        }

        public void ShowFriends() {
            ClearEntries();
            GetEntries(LeaderboardFetchType.Friends);
        }

        private async void GetEntries(LeaderboardFetchType fetchType) {
            if (_leaderboard != null) {
                leaderboardText.text = "FETCHING ...";
                var newEntries = await _leaderboard.GetEntries(fetchType);
                ClearEntries();

                leaderboardText.text = newEntries.Count > 0 ? "" : "NO LEADERBOARD ENTRIES FOUND";

                if (newEntries.Count > 0)
                    foreach (var leaderboardEntry in newEntries) {
                        var entry = Instantiate(leaderboardEntryPrefab, container);
                        entry.GetData(leaderboardEntry);
                    }
            }
        }
    }
}