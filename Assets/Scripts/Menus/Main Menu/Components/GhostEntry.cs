using Core.Replays;
using FdUI;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class GhostEntry : MonoBehaviour {
        [SerializeField] public TMPro.TMP_Text playerName;
        [SerializeField] public TMPro.TMP_Text entryDate;
        [SerializeField] public TMPro.TMP_Text score;
        [SerializeField] public Checkbox checkbox;
        [SerializeField] public Button deleteButton;
        public Replay replay;

        private void Start() {
            var levelCompetitionPanel = GetComponentInParent<LevelCompetitionPanel>(true);
            if (levelCompetitionPanel) deleteButton.onClick.AddListener(() => levelCompetitionPanel.DeleteGhost(this));
        }
    }
}