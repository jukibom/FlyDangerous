using Core.OnlineServices;
using JetBrains.Annotations;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class LeaderboardEntry : MonoBehaviour {

        [CanBeNull] private ILeaderboardEntry _entry;
        [SerializeField] private Text rank;
        [SerializeField] private Text playerName;
        [SerializeField] private FlagIcon flagIcon;
        [SerializeField] private Text score;
        public void GetData(ILeaderboardEntry entry) {
            _entry = entry;
            Refresh();
        }

        private void Refresh() {
            if (_entry != null) {
                rank.text = _entry.Rank.ToString();
                playerName.text = _entry.Player;
                score.text = TimeExtensions.TimeSecondsToString(_entry.Score / 1000f);
                flagIcon.SetFlag(_entry.Flag);
            }
        }
    }
}
