using System;
using System.Collections;
using System.Collections.Generic;
using Core.OnlineServices;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class Leaderboard : MonoBehaviour {
        [SerializeField] private RectTransform container;
        [SerializeField] private LeaderboardEntry leaderboardEntryPrefab;
        [SerializeField] private Text leaderboardText;
        [SerializeField] private GameObject loadingOverlay;
        [CanBeNull] private Coroutine _addLeaderboardEntryCoroutine;

        [CanBeNull] private ILeaderboard _leaderboard;

        public void LoadLeaderboard(ILeaderboard leaderboard) {
            _leaderboard = leaderboard;
            ClearEntries();
            ShowMe();
        }

        public void ClearEntries() {
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

        public void ShowFailed(string message) {
            leaderboardText.text = message;
            loadingOverlay.SetActive(false);
        }

        private async void GetEntries(LeaderboardFetchType fetchType) {
            if (_addLeaderboardEntryCoroutine != null) StopCoroutine(_addLeaderboardEntryCoroutine);

            if (_leaderboard != null) {
                loadingOverlay.SetActive(true);
                leaderboardText.text = "FETCHING ...";
                var newEntries = await _leaderboard.GetEntries(fetchType);
                ClearEntries();

                leaderboardText.text = newEntries.Count > 0 ? "" : "NO LEADERBOARD ENTRIES FOUND";

                // panel may have closed after entries have been fetched
                if (newEntries.Count > 0 && gameObject.activeSelf)
                    _addLeaderboardEntryCoroutine = StartCoroutine(AddEntries(newEntries, () => loadingOverlay.SetActive(false)));
                else
                    loadingOverlay.SetActive(false);
            }
        }

        private IEnumerator AddEntries(List<ILeaderboardEntry> leaderboardEntries, Action onComplete) {
            foreach (var leaderboardEntry in leaderboardEntries) {
                var entry = Instantiate(leaderboardEntryPrefab, container);
                entry.GetData(leaderboardEntry);
                yield return new WaitForEndOfFrame();
            }

            onComplete();
        }
    }
}