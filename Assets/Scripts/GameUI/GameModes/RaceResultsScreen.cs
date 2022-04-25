using System.Collections;
using System.Threading.Tasks;
using Core;
using Core.Player;
using Core.Scores;
using Menus.Main_Menu.Components;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.GameModes {
    public class RaceResultsScreen : MonoBehaviour {
        [SerializeField] private Image resultsScreenBackground;
        [SerializeField] private MedalsScreen medalsScreen;
        [SerializeField] private GameObject uploadScreen;
        [SerializeField] private LevelCompetitionPanel competitionPanel;
        [SerializeField] private GameObject uiButtons;

        public void Hide() {
            resultsScreenBackground.enabled = false;
            medalsScreen.gameObject.SetActive(false);
            uploadScreen.gameObject.SetActive(false);
            competitionPanel.gameObject.SetActive(false);
            uiButtons.gameObject.SetActive(false);
        }

        public void Show(Score score, Score previousBest, bool isValid, string replayFilename, string replayFilepath) {
            resultsScreenBackground.enabled = true;
            StartCoroutine(ShowEndResultsScreen(score, previousBest, isValid, replayFilename, replayFilepath));
        }

        private IEnumerator ShowEndResultsScreen(Score score, Score previousBest, bool isValid, string replayFilename, string replayFilepath) {
            yield return new WaitForSeconds(1f);
            yield return ShowMedalScreen(score, previousBest, isValid);
            yield return new WaitForSecondsRealtime(1);
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                player.User.EnableUIInput();
                player.User.InGameUI.CursorIsActive(true);
                Game.Instance.FreeCursor();
            }

            medalsScreen.gameObject.SetActive(true);

            // TODO: and the rest yay
        }

        private IEnumerator ShowMedalScreen(Score score, Score previousBest, bool isValid) {
            medalsScreen.gameObject.SetActive(true);

            var personalBest = score.PersonalBestTotalTime;
            var previousPersonalBest = previousBest is { HasPlayedPreviously: true } ? previousBest.PersonalBestTotalTime : 0;
            var isNewPersonalBest = previousBest is { HasPlayedPreviously: false } || previousBest?.PersonalBestTotalTime > personalBest;
            var levelData = Game.Instance.LoadedLevelData;

            var authorTargetTime = Score.AuthorTimeTarget(levelData);
            var goldTargetTime = Score.GoldTimeTarget(levelData);
            var silverTargetTime = Score.SilverTimeTarget(levelData);
            var bronzeTargetTime = Score.BronzeTimeTarget(levelData);

            uint medalCount = 0;
            if (personalBest < bronzeTargetTime)
                medalCount++;
            if (personalBest < silverTargetTime)
                medalCount++;
            if (personalBest < goldTargetTime)
                medalCount++;
            if (personalBest < authorTargetTime)
                medalCount++;

            yield return medalsScreen.ShowAnimation(medalCount, isNewPersonalBest, personalBest, previousPersonalBest, isValid);
        }

        private async Task UploadLeaderboardResult(float timeSeconds, string levelHash, string replayFileName, string replayFilePath) {
            if (FdNetworkManager.Instance.HasLeaderboardServices && replayFileName != "" && replayFilePath != "") {
                var flagId = Flag.FromFilename(Preferences.Instance.GetString("playerFlag")).FixedId;
                var leaderboard = await FdNetworkManager.Instance.OnlineService!.Leaderboard!.FindOrCreateLeaderboard(levelHash);
                var timeMilliseconds = timeSeconds * 1000;

                // TODO: This can ABSOLUTELY fail, handle it in the end screen!
                await leaderboard.UploadScore((int)timeMilliseconds, flagId, replayFilePath, replayFileName);
                Debug.Log("Leaderboard upload succeeded");
            }
        }
    }
}