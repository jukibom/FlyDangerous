using Core;
using Core.Replays;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Pause_Menu {
    public class LiveGhostEntry : MonoBehaviour {
        [SerializeField] public Text playerName;
        [SerializeField] public Text score;
        [SerializeField] public Button spectateButton;
        public ReplayTimeline ReplayTimeline { get; set; }

        private void Start() {
            spectateButton.onClick.AddListener(() => {
                if (ReplayTimeline.ShipReplayObject is ShipGhost ghost) {
                    ReplayPrioritizer.Instance.SpectateGhost(ghost);
                }
            });
        }
    }
}