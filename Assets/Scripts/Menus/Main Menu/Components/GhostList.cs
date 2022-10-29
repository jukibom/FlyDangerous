using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.MapData;
using Core.Replays;
using JetBrains.Annotations;
using Misc;
using UnityEngine;

namespace Menus.Main_Menu.Components {
    public class GhostList : MonoBehaviour {
        [SerializeField] private RectTransform ghostEntryContainer;
        [SerializeField] private GhostEntry ghostEntryPrefab;
        [SerializeField] private GameObject noGhostText;
        [SerializeField] private GameObject loadingOverlay;
        private Coroutine _addGhostsCoroutine;

        public IEnumerable<GhostEntry> GhostEntries => ghostEntryContainer.GetComponentsInChildren<GhostEntry>();

        private void OnEnable() {
            noGhostText.SetActive(true);
        }

        public void PopulateGhostsForLevel(LevelData levelData, Action onComplete) {
            loadingOverlay.SetActive(true);
            if (_addGhostsCoroutine != null) StopCoroutine(_addGhostsCoroutine);

            ClearGhosts();

            var ghosts = Replay.ReplaysForLevel(levelData);
            if (ghosts.Count > 0) noGhostText.SetActive(false);
            ghosts = ghosts.OrderBy(r => r.ScoreData.raceTime).ToList();

            _addGhostsCoroutine = StartCoroutine(AddGhosts(ghosts, () => {
                loadingOverlay.SetActive(false);
                onComplete();
            }));
        }

        public void ClearGhosts() {
            foreach (var ghostEntry in GhostEntries) Destroy(ghostEntry.gameObject);
        }

        /**
         * Return the element below, if it exists, or the element above, if it exists.
         */
        [CanBeNull]
        public GhostEntry GetNearest(GhostEntry nextToGhostEntry) {
            GhostEntry ge = null;
            var entries = ghostEntryContainer.GetComponentsInChildren<GhostEntry>();
            for (var i = 0; i < entries.Length; i++)
                if (entries[i] == nextToGhostEntry) {
                    if (entries.Length > i + 1)
                        ge = entries[i + 1];
                    else if (i > 0) ge = entries[i - 1];
                }

            return ge;
        }

        private IEnumerator AddGhosts(List<Replay> ghosts, Action onComplete) {
            foreach (var replay in ghosts) {
                var ghostEntry = Instantiate(ghostEntryPrefab);
                ghostEntry.GetComponent<RectTransform>().SetParent(ghostEntryContainer, false);
                ghostEntry.playerName.text = replay.ShipProfile.playerName;
                ghostEntry.score.text = TimeExtensions.TimeSecondsToString(replay.ScoreData.raceTime);
                ghostEntry.entryDate.text = replay.ReplayMeta.CreationDate.ToShortDateString();
                ghostEntry.replay = replay;
                ghostEntry.checkbox.isChecked = false;
                yield return new WaitForEndOfFrame();
            }

            onComplete();
        }
    }
}