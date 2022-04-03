using System;
using System.Collections.Generic;
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
            foreach (var ghostEntry in ghostEntryContainer.GetComponentsInChildren<GhostEntry>()) {
                Destroy(ghostEntry.gameObject);
            }
            
            _replays = Replay.ReplaysForLevel(level.Data);
            if (_replays.Count > 0) {
                noGhostText.SetActive(false);
            }
            
            foreach (var replay in _replays) {
                var ghost = Instantiate(ghostEntryPrefab);
                ghost.GetComponent<RectTransform>().SetParent(ghostEntryContainer, false);

                ghost.playerName.text = replay.ShipProfile.playerName;
                ghost.score.text = TimeExtensions.TimeSecondsToString(replay.ScoreData.raceTime);
                ghost.entryDate.text = replay.ReplayMeta.CreationDate.ToShortDateString();
            }
        }
    }
}
