using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core;
using Core.MapData;
using Core.Replays;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class LevelCompetitionPanel : MonoBehaviour {
        [SerializeField] private GhostList ghostList;
        [SerializeField] private Leaderboard leaderboard;

        [Label("Used if deleting a ghost entry and there's none others to select")] [SerializeField]
        private Button defaultSelectedElement;

        private LevelData _levelData;

        public void Populate(LevelData levelData) {
            _levelData = levelData;
            PopulateLeaderboardForLevel();
            PopulateGhostsForLevel();
        }

        public List<Replay> GetSelectedReplays() {
            return ghostList.GetComponentsInChildren<GhostEntry>().ToList().FindAll(entry => entry.checkbox.isChecked).ConvertAll(entry => entry.replay);
        }

        public async void DownloadGhost(LeaderboardEntry leaderboardEntry) {
            // store the current selected UI element to return to later, if nothing is selected
            var currentSelectedElement = EventSystem.current.currentSelectedGameObject;

            var currentSelectedReplays = GetSelectedReplays();
            var directory = Path.Combine(Replay.ReplayDirectory, _levelData.LevelHash());
            var filePath = await leaderboardEntry.DownloadReplay(directory);
            if (filePath != "") {
                var newReplay = Replay.LoadFromFilepath(filePath);
                PopulateGhostsForLevel();

                SelectReplaysInList(currentSelectedReplays);
                SelectReplayInList(newReplay);
            }

            // restore selection if the user has moved over to a ghost element which has subsequently been replaced with refreshed state
            // OF COURSE we need to wait a frame for the event system to do it's whatever-the-hell
            IEnumerator RestoreSelectedIfNeeded() {
                yield return new WaitForEndOfFrame();
                if (EventSystem.current.currentSelectedGameObject == null) EventSystem.current.SetSelectedGameObject(currentSelectedElement);
            }

            StartCoroutine(RestoreSelectedIfNeeded());
        }

        public void DeleteGhost(GhostEntry ghostEntry) {
            // why does this event system hate me
            IEnumerator SelectElement(Button element) {
                yield return new WaitForEndOfFrame();
                element.Select();
            }

            var nearestElement = ghostList.GetNearest(ghostEntry);

            if (nearestElement != null)
                StartCoroutine(SelectElement(nearestElement.deleteButton));
            else
                StartCoroutine(SelectElement(defaultSelectedElement));

            ghostEntry.replay.Delete();
            Destroy(ghostEntry.gameObject);
        }

        private void PopulateGhostsForLevel() {
            ghostList.PopulateGhostsForLevel(_levelData);
            if (Game.Instance.ActiveGameReplays != null)
                SelectReplaysInList(Game.Instance.ActiveGameReplays);
        }

        private async void PopulateLeaderboardForLevel() {
            leaderboard.gameObject.SetActive(FdNetworkManager.Instance.HasLeaderboardServices);
            if (FdNetworkManager.Instance.HasLeaderboardServices) {
                var leaderboardData = await FdNetworkManager.Instance.OnlineService!.Leaderboard!.FindOrCreateLeaderboard(_levelData.LevelHash());
                leaderboard.LoadLeaderboard(leaderboardData);
            }
        }

        private void SelectReplaysInList(List<Replay> replays) {
            ghostList.GetComponentsInChildren<GhostEntry>().ToList().ForEach(ghost => {
                if (replays.Exists(replay => replay.Hash == ghost.replay.Hash))
                    ghost.checkbox.isChecked = true;
            });
        }

        private void SelectReplayInList(Replay replay) {
            ghostList.GetComponentsInChildren<GhostEntry>().ToList().ForEach(ghost => {
                if (ghost.replay.Hash == replay.Hash) ghost.checkbox.isChecked = true;
            });
        }
    }
}