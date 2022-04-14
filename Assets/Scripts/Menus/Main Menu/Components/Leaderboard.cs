using Core;
using Core.OnlineServices;
using Core.Player;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class Leaderboard : MonoBehaviour {

        [CanBeNull] private ILeaderboard _leaderboard;
        [SerializeField] private RectTransform container;
        [SerializeField] private LeaderboardEntry leaderboardEntryPrefab;
        [SerializeField] private Text leaderboardText;
        
        public void LoadLeaderboard(ILeaderboard leaderboard) {
            _leaderboard = leaderboard;
            leaderboardText.text = "FETCHING ...";
            ClearEntries();
            GetEntries();
        }

        private async void GetEntries() {
            if (_leaderboard != null) {
                var newEntries = await _leaderboard.GetEntries();
                leaderboardText.text = (newEntries.Count > 0) ? "" : "NO LEADERBOARD ENTRIES FOUND";

                if (newEntries.Count > 0) {
                    foreach (var leaderboardEntry in newEntries) {
                        var entry = Instantiate(leaderboardEntryPrefab, container);
                        entry.GetData(leaderboardEntry);
                    }
                }
            }
        }

        private void ClearEntries() {
            var entries = container.gameObject.GetComponentsInChildren<LeaderboardEntry>();
            Debug.Log(entries.Length);
            foreach (var leaderboardEntry in entries) {
                Destroy(leaderboardEntry.gameObject);
            }
        }

        public async void TestScoreUpload(int score) {
            await _leaderboard.UploadScore(score, Flag.FromFilename(Preferences.Instance.GetString("playerFlag")));
            ClearEntries();
            GetEntries();
        }
    }
}
