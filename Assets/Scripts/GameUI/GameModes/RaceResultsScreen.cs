using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.MapData;
using Core.Player;
using Core.Replays;
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
        [SerializeField] private Button defaultSelectedButton;
        [SerializeField] private AudioListener temporaryAudioListener;

        private void OnEnable() {
            Game.OnRestart += SetReplaysAndHideCursor;
        }

        private void OnDisable() {
            Game.OnRestart -= SetReplaysAndHideCursor;
        }

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

        private IEnumerator ShowEndResultsScreen(Score score, Score previousBest, bool isValid, string replayFileName, string replayFilePath) {
            var levelData = Game.Instance.LoadedLevelData;

            yield return new WaitForSeconds(1f);
            yield return ShowMedalScreen(score, previousBest, isValid);
            yield return new WaitForSecondsRealtime(1);
            medalsScreen.gameObject.SetActive(false);

            var uploadTask = UploadLeaderboardResultIfValid(score.PersonalBestTotalTime, levelData.LevelHash(), replayFileName,
                replayFilePath);
            yield return new WaitUntil(() => uploadTask.IsCompleted);

            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                FindObjectOfType<InGameUI>()?.OnPauseToggle(true);
                Game.Instance.FreeCursor();
                player.User.EnableUIInput();
                player.User.ResetMouseToCentre();
                player.User.restartEnabled = true;
            }

            // TODO: animation
            competitionPanel.gameObject.SetActive(true);
            uiButtons.gameObject.SetActive(true);
            defaultSelectedButton.Select();

            competitionPanel.Populate(levelData);
        }

        private IEnumerator ShowMedalScreen(Score score, Score previousBest, bool isValid) {
            medalsScreen.gameObject.SetActive(true);

            var result = score.PersonalBestTotalTime;
            var previousPersonalBest = previousBest is { HasPlayedPreviously: true } ? previousBest.PersonalBestTotalTime : 0;
            var isNewPersonalBest = previousBest is { HasPlayedPreviously: false } || previousBest?.PersonalBestTotalTime > result;
            var levelData = Game.Instance.LoadedLevelData;

            var authorTargetTime = Score.AuthorTimeTarget(levelData);
            var goldTargetTime = Score.GoldTimeTarget(levelData);
            var silverTargetTime = Score.SilverTimeTarget(levelData);
            var bronzeTargetTime = Score.BronzeTimeTarget(levelData);

            uint medalCount = 0;
            if (result < bronzeTargetTime)
                medalCount++;
            if (result < silverTargetTime)
                medalCount++;
            if (result < goldTargetTime)
                medalCount++;
            if (result < authorTargetTime)
                medalCount++;

            yield return medalsScreen.ShowAnimation(medalCount, isNewPersonalBest, result, previousPersonalBest, isValid);
        }

        private async Task UploadLeaderboardResultIfValid(float timeSeconds, string levelHash, string replayFileName, string replayFilePath) {
            if (FdNetworkManager.Instance.HasLeaderboardServices && replayFileName != "" && replayFilePath != "") {
                // TODO: menu animation
                uploadScreen.gameObject.SetActive(true);

                var flagId = Flag.FromFilename(Preferences.Instance.GetString("playerFlag")).FixedId;
                var leaderboard = await FdNetworkManager.Instance.OnlineService!.Leaderboard!.FindOrCreateLeaderboard(levelHash);
                var timeMilliseconds = timeSeconds * 1000;

                try {
                    await leaderboard.UploadScore((int)timeMilliseconds, flagId, replayFilePath, replayFileName);
                    Debug.Log("Leaderboard upload succeeded");
                }
                catch {
                    // TODO: Retry screen
                    Debug.Log("Failed to upload!");
                }

                uploadScreen.gameObject.SetActive(false);
            }
        }

        public void QuitToMenu() {
            Game.Instance.QuitToMenu();
        }

        public void Retry() {
            SetReplaysAndHideCursor();
            Game.Instance.RestartSession();
        }

        // hide the mouse and do all the things that normally happens when un-pausing
        private void SetReplaysAndHideCursor() {
            // overwrite ghosts if the panel is open (this can be triggered by a restart in general)
            if (competitionPanel.isActiveAndEnabled) Game.Instance.ActiveGameReplays = competitionPanel.GetSelectedReplays();
            FindObjectOfType<InGameUI>()?.OnPauseToggle(false);
        }

        public void NextLevel() {
            // TODO: probably something better than this hot bullshit but I am very much at the end of my tether (esp. the audio listener)
            if (Game.Instance.loadedMainLevel != null) {
                var nextLevel = Level.FromId(Game.Instance.loadedMainLevel.Id + 1);

                var replaysForNextLevel = Replay.ReplaysForLevel(nextLevel.Data).OrderBy(r => r.ScoreData.raceTime).ToList();
                Game.Instance.ActiveGameReplays = new List<Replay>();
                if (replaysForNextLevel.Count > 0) Game.Instance.ActiveGameReplays.Add(replaysForNextLevel.First());

                FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, nextLevel.Data);
                Game.Instance.loadedMainLevel = nextLevel;

                // continue to play music while killing the ship and destroying the world (yeet ourselves off the ship!)
                var lastBastionOfHope = FindObjectOfType<World>();
                if (lastBastionOfHope) {
                    transform.parent = lastBastionOfHope.transform;
                    temporaryAudioListener.enabled = true;
                }
            }
        }
    }
}