using System.IO;
using System.Threading.Tasks;
using Core.OnlineServices;
using Core.Player;
using JetBrains.Annotations;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class LeaderboardEntry : MonoBehaviour {
        [SerializeField] private Text rank;
        [SerializeField] private Text playerName;
        [SerializeField] private FlagIcon flagIcon;
        [SerializeField] private Text score;
        [SerializeField] private Button downloadButton;

        [CanBeNull] private ILeaderboardEntry _entry;

        private void Start() {
            var levelCompetitionPanel = GetComponentInParent<LevelCompetitionPanel>(true);
            if (levelCompetitionPanel) downloadButton.onClick.AddListener(() => levelCompetitionPanel.DownloadGhost(this));
        }

        public void GetData(ILeaderboardEntry entry) {
            _entry = entry;

            // show highlight if it's the players' entry
            if (Player.LocalPlayerName.Equals(entry.Player))
                GetComponent<Image>().enabled = true;

            Refresh();
        }

        private void Refresh() {
            if (_entry != null) {
                rank.text = _entry.Rank.ToString();
                playerName.text = _entry.Player;
                score.text = TimeExtensions.TimeSecondsToString(_entry.Score / 1000f);
                flagIcon.SetFlag(Flag.FromFixedId(_entry.FlagId));
            }
        }

        public async Task<string> DownloadReplay(string toLocation) {
            if (_entry != null) {
                var onlineFile = await _entry.Replay();

                var saveLoc = Path.Combine(toLocation, onlineFile.Filename);
                var directoryLoc = Path.GetDirectoryName(saveLoc);
                if (directoryLoc != null) Directory.CreateDirectory(directoryLoc);

                await using var file = new FileStream(saveLoc, FileMode.Create, FileAccess.Write);
                var bytes = new byte[onlineFile.Data.Length];
                onlineFile.Data.Read(bytes, 0, (int)onlineFile.Data.Length);
                file.Write(bytes, 0, bytes.Length);
                file.Close();
                onlineFile.Data.Close();

                return saveLoc;
            }

            return "";
        }
    }
}