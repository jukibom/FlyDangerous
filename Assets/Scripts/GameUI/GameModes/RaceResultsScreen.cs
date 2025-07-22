using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.MapData;
using Core.Player;
using Core.Replays;
using Core.Scores;
using JetBrains.Annotations;
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
        [SerializeField] private Button quitButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private AudioListener temporaryAudioListener;

        private Action _onStart;
        
        [CanBeNull] private Level CurrentLevel => Game.Instance.loadedMainLevel;
        private Level NextLevel => Level.FromId(CurrentLevel?.Id + 1 ?? 0);
        private bool IsNextLevelValid => NextLevel.Id > (CurrentLevel?.Id ?? 0) && NextLevel.GameType == CurrentLevel?.GameType;

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

        public void RunLevelComplete(Score score, Score previousBest, bool isValid, string replayFilename, string replayFilepath) {
            startButton.gameObject.SetActive(false);
            quitButton.gameObject.SetActive(true);
            retryButton.gameObject.SetActive(true);
            
            resultsScreenBackground.enabled = true;
            StartCoroutine(ShowEndResultsScreen(score, previousBest, isValid, replayFilename, replayFilepath));
        }

        public void ShowCompetitionPanel(Action onStart) {
            _onStart = onStart;
            
            startButton.gameObject.SetActive(true);
            quitButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(false);
            nextLevelButton.gameObject.SetActive(false);
            startButton.Select();
            ShowCompetitionPanelInternal();
        }

        private void ShowCompetitionPanelInternal() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                FindObjectOfType<InGameUI>()?.OnPauseToggle(true);
                Game.Instance.FreeCursor();
                player.User.EnableUIInput();
                player.User.ResetMouseToCentre();
                player.User.restartEnabled = true;
            }
            
            var levelData = Game.Instance.LoadedLevelData;
            competitionPanel.gameObject.SetActive(true);
            uiButtons.gameObject.SetActive(true);
            
            competitionPanel.Populate(levelData);
        }

        private IEnumerator ShowEndResultsScreen(Score score, Score previousBest, bool isValid, string replayFileName, string replayFilePath) {
            var levelData = Game.Instance.LoadedLevelData;

            yield return new WaitForSeconds(1f);
            yield return ShowMedalScreen(score, previousBest, isValid);
            yield return new WaitForSecondsRealtime(1);
            medalsScreen.gameObject.SetActive(false);

            var uploadTask = UploadLeaderboardResultIfValid(score.PersonalBestScore, levelData.LevelHash(), replayFileName,
                replayFilePath);
            yield return new WaitUntil(() => uploadTask.IsCompleted);

            ShowCompetitionPanelInternal();
            nextLevelButton.gameObject.SetActive(IsNextLevelValid);
        }

        private IEnumerator ShowMedalScreen(Score score, Score previousBest, bool isValid) {
            medalsScreen.gameObject.SetActive(true);

            var result = score.PersonalBestScore;
            var previousPersonalBest = previousBest is { HasPlayedPreviously: true } ? previousBest.PersonalBestScore : 0;
            var isNewPersonalBest = previousBest is { HasPlayedPreviously: false } || previousBest?.PersonalBestScore > result;
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

        public void StartGame() {
            var player = FdPlayer.FindLocalShipPlayer;
            player?.User.DisableUIInput();
            SetReplaysAndHideCursor();
            _onStart?.Invoke();
            Hide();
        }

        // hide the mouse and do all the things that normally happens when un-pausing
        private void SetReplaysAndHideCursor() {
            SetReplaysFromPanel();
            FindObjectOfType<InGameUI>()?.OnPauseToggle(false);
        }

        public void SetReplaysFromPanel() {
            // overwrite ghosts if the panel is open (this can be triggered by a restart in general)
            if (competitionPanel.isActiveAndEnabled) Game.Instance.ActiveGameReplays = competitionPanel.GetSelectedReplays();
        }

        public void LoadNextLevel() {
            // if we've wrapped around or the game types don't match, yeet to main menu
            if (!IsNextLevelValid) {
                QuitToMenu();
                return;
            }

            if (CurrentLevel != null) {
                var replaysForNextLevel = Replay.ReplaysForLevel(NextLevel.Data).OrderBy(r => r.ScoreData.raceTime).ToList();
                Game.Instance.ActiveGameReplays = new List<Replay>();
                if (replaysForNextLevel.Count > 0) Game.Instance.ActiveGameReplays.Add(replaysForNextLevel.First());

                FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, NextLevel.Data);
                Game.Instance.loadedMainLevel = NextLevel;

                // TODO: probably something better than this hot bullshit but I am very much at the end of my tether (esp. the audio listener)
                // continue to play music while killing the ship and destroying the world (yeet ourselves off the ship! world will die taking this with it)
                var lastBastionOfHope = FindObjectOfType<World>();
                if (lastBastionOfHope) {
                    transform.parent = lastBastionOfHope.transform;
                    temporaryAudioListener.enabled = true;
                }
            }
            else {
                QuitToMenu();
            }
        }
    }
}