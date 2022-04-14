using System.Collections.Generic;
using System.Linq;
using Core.MapData;
using Core.Replays;
using Misc;
using UnityEngine;

namespace Menus.Main_Menu.Components {
    public class GhostList : MonoBehaviour {
        [SerializeField] private RectTransform ghostEntryContainer;
        [SerializeField] private GhostEntry ghostEntryPrefab;
        [SerializeField] private GameObject noGhostText;

        private List<Replay> _replays;

        private void OnEnable() {
            noGhostText.SetActive(true);
        }

        public void PopulateGhostsForLevel(Level level) {
            foreach (var ghostEntry in ghostEntryContainer.GetComponentsInChildren<GhostEntry>()) Destroy(ghostEntry.gameObject);

            _replays = Replay.ReplaysForLevel(level.Data);
            if (_replays.Count > 0) noGhostText.SetActive(false);

            _replays = _replays.OrderBy(r => r.ScoreData.raceTime).ToList();

            foreach (var replay in _replays) {
                var ghostEntry = Instantiate(ghostEntryPrefab);
                ghostEntry.GetComponent<RectTransform>().SetParent(ghostEntryContainer, false);
                ghostEntry.playerName.text = replay.ShipProfile.playerName;
                ghostEntry.score.text = TimeExtensions.TimeSecondsToString(replay.ScoreData.raceTime);
                ghostEntry.entryDate.text = replay.ReplayMeta.CreationDate.ToShortDateString();
                ghostEntry.replay = replay;

                // enable the best score by default
                ghostEntry.isEnabled.isChecked = _replays.First() == replay;
            }
        }
    }
}