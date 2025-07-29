using Core;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Pause_Menu {
    public class LiveGhostPanel : MonoBehaviour {
        [SerializeField] private LiveGhostEntry _ghostEntryPrefab;
        [SerializeField] private Transform _container;
        [SerializeField] private Button _stopSpectatingButton;

        private void Start() {
            _stopSpectatingButton.onClick.AddListener(() => {
                ReplayPrioritizer.Instance.StopSpectating();
                _stopSpectatingButton.interactable = false;
            });
        }

        public void Refresh() {
            for (var i = _container.childCount - 1; i >= 0; i--) {
                Destroy(_container.GetChild(i).gameObject);
            }

            foreach (var instanceReplay in ReplayPrioritizer.Instance.Replays) {
                var entry = Instantiate(_ghostEntryPrefab, _container);
                if (instanceReplay.Replay != null) {
                    entry.playerName.text = instanceReplay.Replay.ShipProfile.playerName;
                    entry.score.text = TimeExtensions.TimeSecondsToStringWithMilliseconds(instanceReplay.Replay.ScoreData.raceTime);
                    entry.spectateButton.onClick.AddListener(() => _stopSpectatingButton.interactable = true);
                }
                entry.ReplayTimeline = instanceReplay;
            }
            
            _stopSpectatingButton.interactable = ReplayPrioritizer.Instance.IsSpectating;
        }
    }
}