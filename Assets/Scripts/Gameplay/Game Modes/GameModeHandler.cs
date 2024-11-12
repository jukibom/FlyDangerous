using System.Collections;
using System.Collections.Generic;
using System.IO;
using Audio;
using Core;
using Core.MapData;
using Core.Player;
using Core.Replays;
using Core.Scores;
using Core.ShipModel;
using CustomWebSocketSharp;
using Gameplay.Game_Modes.Components;
using Gameplay.Game_Modes.Components.Interfaces;
using GameUI;
using JetBrains.Annotations;
using Misc;
using UnityEngine;

namespace Gameplay.Game_Modes {
    [RequireComponent(typeof(ReplayRecorder))]
    public class GameModeHandler : MonoBehaviour {
        private ReplayRecorder _replayRecorder;
        private List<ShipGhost> ActiveGhosts { get; set; } = new();

        // used to make absolutely sure there's no weirdness going on at the start
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        private IGameMode _gameMode;
        private InGameUI _inGameUI;
        private Track _track;
        private LevelData _levelData;
        private bool _isValid = true;

        // Handler refs
        [CanBeNull] private IGameModeWithScore _gameModeWithScore;
        [CanBeNull] private IGameModeWithCountdown _gameModeWithCountdown;
        [CanBeNull] private IGameModeWithCheckpoints _gameModeWithCheckpoint;
        [CanBeNull] private IGameModeWithTimer _gameModeWithTimer;

        // Concrete implementations
        private GameModeScore _gameModeScore;
        private GameModeTimer _gameModeTimer;
        private GameModeLifecycle _gameModeLifecycle;
        private GameModeCountdown _gameModeCountdown;

        // lifecycle
        private Coroutine _startSequenceCoroutine;
        private Coroutine _showLevelAndMusicName;
        private bool _gameStarted;

        private ShipPlayer LocalPlayer { get; set; }
        public bool ShipActive => LocalPlayer != null && LocalPlayer.ShipPhysics.ShipActive;
        public bool HasStarted => ShipActive && _gameStarted;

        private void OnEnable() {
            _replayRecorder = GetComponent<ReplayRecorder>();
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
            Game.OnRestart += Restart;
        }

        private void OnDisable() {
            if (_track != null) _track.OnCheckpointHit -= OnCheckpointHit;
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
            Game.OnRestart -= Restart;
        }

        private void FixedUpdate() {
            if (ShipActive) {
                _gameModeTimer.Tick(Time.fixedDeltaTime);
                _gameMode.OnFixedUpdate();
            }
        }

        public void InitialiseGameMode(ShipPlayer localPlayer, LevelData levelData, IGameMode gameMode, InGameUI inGameUI, Track track) {
            CheckValidity();

            _gameModeWithScore = null;
            _gameModeWithCountdown = null;
            _gameModeWithCheckpoint = null;
            _gameModeWithTimer = null;
            inGameUI.GameModeUIHandler.GameModeUIText.HideAll();
            inGameUI.HideWorldCanvas();

            _gameModeScore = new GameModeScore(gameMode, levelData);
            _gameModeTimer = new GameModeTimer();
            _gameModeLifecycle = new GameModeLifecycle(this, localPlayer, Restart, Complete);
            _gameModeCountdown = new GameModeCountdown();

            LocalPlayer = localPlayer;
            _gameMode = gameMode;
            _inGameUI = inGameUI;
            _track = track;
            _levelData = levelData;

            _track.OnCheckpointHit += OnCheckpointHit;

            // Vaguely dependency-injection fun!
            _gameMode.GameModeLifecycle = _gameModeLifecycle;
            _gameMode.GameModeUIHandler = inGameUI.GameModeUIHandler;

            if (_gameMode is IGameModeWithScore gameModeWithScore)
                _gameModeWithScore = gameModeWithScore;
            if (_gameMode is IGameModeWithCountdown gameModeWithCountdown)
                _gameModeWithCountdown = gameModeWithCountdown;
            if (_gameMode is IGameModeWithCheckpoints gameModeWithCheckpoint)
                _gameModeWithCheckpoint = gameModeWithCheckpoint;
            if (_gameMode is IGameModeWithTimer gameModeWithTimer)
                _gameModeWithTimer = gameModeWithTimer;

            if (_gameModeWithScore != null) _gameModeWithScore.GameModeScore = _gameModeScore;
            if (_gameModeWithCheckpoint != null) _gameModeWithCheckpoint.GameModeCheckpoints = _track.GameModeCheckpoints;
            if (_gameModeWithTimer != null) _gameModeWithTimer.GameModeTimer = _gameModeTimer;

            _gameMode.OnInitialise();

            if (_showLevelAndMusicName != null) StopCoroutine(_showLevelAndMusicName);
            _showLevelAndMusicName = StartCoroutine(ShowLevelAndMusicName());
        }

        public void StartGame() {
        }

        public void Begin() {
            _startPosition = LocalPlayer.AbsoluteWorldPosition;
            _startRotation = LocalPlayer.transform.rotation;

            HandleStartSequence();
        }

        private void Restart() {
            StopGhosts();
            LocalPlayer.User.DisableUIInput();
            LocalPlayer.SetNightVisionEnabled(false);
            _inGameUI.GameModeUIHandler.RaceResultsScreen.SetReplaysFromPanel();
            _inGameUI.GameModeUIHandler.GameModeUIText.HideGameUIText(false);
            _inGameUI.GameModeUIHandler.RaceResultsScreen.Hide();
            _inGameUI.HideWorldCanvas();
            _gameModeScore.Reset();
            _gameModeTimer.Reset();
            _gameMode.OnRestart();
            HandleStartSequence();
        }

        public void Complete() {
            CheckValidity();
            _gameModeTimer.Stop();
            _gameMode.OnComplete();

            if (_gameModeWithScore != null) {
                var score = _gameModeScore.Score;
                var previousScore = _gameModeScore.PreviousScore;
                var replayFileName = "";
                var replayFilePath = "";

                // if new valid run OR better score, save!
                if (_isValid)
                    if (previousScore.PersonalBestScore == 0 || score.PersonalBestScore < previousScore.PersonalBestScore) {
                        var scoreData = score.Save(Game.Instance.LoadedLevelData);
                        Score.SaveToDisk(scoreData, Game.Instance.LoadedLevelData);

                        if (_replayRecorder) {
                            _replayRecorder.StopRecording();
                            var levelHash = Game.Instance.LoadedLevelData.LevelHash();
                            var replay = _replayRecorder.Replay;
                            replayFileName = replay?.Save(scoreData);
                            replayFilePath = Path.Combine(Replay.ReplayDirectory, levelHash, replayFileName ?? string.Empty);
                        }
                    }

                _inGameUI.GameModeUIHandler.ShowResultsScreen(score, previousScore, _isValid, replayFileName, replayFilePath);
            }
        }

        public void Quit() {
            // premature quit out
            if (_startSequenceCoroutine != null) StopCoroutine(_startSequenceCoroutine);
            if (_showLevelAndMusicName != null) StopCoroutine(_showLevelAndMusicName);

            // nuke all ghosts and references
            StopGhosts();

            // clear out any old references
            _gameModeWithCountdown = null;
            _gameModeWithCheckpoint = null;
            _gameModeWithTimer = null;
        }

        private void OnCheckpointHit(Checkpoint checkpoint, float excessTimeToHitSeconds) {
            var hitTimeSeconds = _gameModeTimer.CurrentSessionTimeSeconds + excessTimeToHitSeconds;
            _gameModeWithCheckpoint?.OnCheckpointHit(checkpoint, hitTimeSeconds);
            if (checkpoint.Type == CheckpointType.End)
                _gameModeWithCheckpoint?.OnLastCheckpointHit(hitTimeSeconds);
        }

        private void OnGameSettingsApplied() {
            if (_gameMode != null)
                _gameMode.OnGameSettingsApplied();
            CheckValidity();
        }

        private void HandleStartSequence() {
            if (_startSequenceCoroutine != null) StopCoroutine(_startSequenceCoroutine);

            // reset this flag on start, it's checked at the end of any game mode
            _isValid = IsValid();
            _gameModeLifecycle.DisableAllShipInput();
            LocalPlayer.ShipPhysics.ShipActive = false;

            IEnumerator StartSequence() {
                _gameModeLifecycle.EnableGameInput();
                _gameModeLifecycle.DisableAllShipInput();
                _inGameUI.ShipStats.ForceHidden = true;

                yield return YieldExtensions.WaitForFixedFrames(2);
                yield return WaitForBoostButtonIfNeeded();

                LocalPlayer.ShipPhysics.ShipActive = true;
                // Tiny wait after ship init to smooth over instant input 
                yield return YieldExtensions.WaitForFixedFrames(20);

                // This is the part where the game mode actually starts, irrespective of countdown 
                // (there may be additional UI etc work to do here). Game modes must account for their timers,
                // but they also dictate the countdown time.
                _gameModeTimer.Start(_gameMode);
                _inGameUI.ShipStats.ForceHidden = false;
                _inGameUI.IndicatorSystem.gameObject.SetActive(true);
                _inGameUI.ShowWorldCanvas(true);
                StartReplayRecordIfSupported();
                StartGhostsIfSupported();
                _gameModeScore.Reset();
                _gameMode.OnBegin();
                _inGameUI.GameModeUIHandler.GameModeUIText.ShowGameUIText();

                yield return StartCountdownIfRequired();

                // make absolutely double-decker sure the player couldn't get away before the timer said so
                if (_gameMode.HasFixedStartLocation) {
                    LocalPlayer.SetTransformWorld(_startPosition, _startRotation);
                    LocalPlayer.ShipPhysics.ResetPhysics(false);
                }

                _gameModeLifecycle.EnableShipInput();
            }

            _startSequenceCoroutine = StartCoroutine(StartSequence());
        }

        private IEnumerator StartCountdownIfRequired() {
            if (_gameModeWithCountdown != null) {
                _gameModeWithCountdown.CountdownStarted();
                yield return _gameModeCountdown.CountdownWithSound(_gameModeWithCountdown.StartingCountdownTime, timeRemaining => {
                    if (timeRemaining <= 0)
                        _gameModeWithCountdown.CountdownComplete();
                    else if (_gameModeWithCountdown.AllowEarlyBoost && timeRemaining <= 1) _gameModeLifecycle.EnableShipBoostInput();
                });
            }

            _gameStarted = true;
        }

        private IEnumerator WaitForBoostButtonIfNeeded() {
            // handle boost button if required
            if (_gameMode.RequireBoostHeldToStart) {
                float timeSeconds = 0;
                var showText = false;
                while (!LocalPlayer.User.BoostButtonHeld) {
                    // wait 2 seconds before showing boost text
                    while (timeSeconds < 2) {
                        timeSeconds += Time.deltaTime;
                        if (LocalPlayer.User.BoostButtonHeld) break;
                        yield return new WaitForEndOfFrame();
                    }

                    yield return new WaitForFixedUpdate();

                    // show the text once and only once if after 2 seconds the user still isn't holding the button
                    if (!showText && !LocalPlayer.User.BoostButtonHeld) {
                        _inGameUI.GameModeUIHandler.HoldBoostButtonText.ShowHoldBoostText();
                        showText = true;
                    }
                }

                _inGameUI.GameModeUIHandler.HoldBoostButtonText.HideHoldBoostText();
            }
        }

        private void StartReplayRecordIfSupported() {
            if (_gameMode.SupportsReplays) {
                _replayRecorder.CancelRecording();
                _replayRecorder.StartNewRecording(LocalPlayer.ShipPhysics);
            }
        }

        private void StartGhostsIfSupported() {
            if (_gameMode.SupportsReplays)
                foreach (var activeReplay in Game.Instance.ActiveGameReplays)
                    ActiveGhosts.Add(Game.Instance.LoadGhost(activeReplay));
        }

        private void StopGhosts() {
            foreach (var shipGhost in ActiveGhosts)
                Game.Instance.RemoveGhost(shipGhost);
            ActiveGhosts = new List<ShipGhost>();
        }

        // remember the last check and logical AND with new check - any time during the game mode that it's invalid will therefore invalidate the run.
        private void CheckValidity() {
            _isValid = _isValid && IsValid();
        }

        private bool IsValid() {
            return !Application.version.Contains("-dev") && Game.Instance.ShipParameters.ToJsonString().Equals(Game.Instance.LoadedLevelData.shipParameters.ToJsonString());
        }

        private IEnumerator ShowLevelAndMusicName() {
            var bottomCanvasGroup = _inGameUI.GameModeUIHandler.LevelDetailsCanvasGroup;
            var bottomLeftText = _inGameUI.GameModeUIHandler.LevelNameText;
            var bottomRightText = _inGameUI.GameModeUIHandler.MusicNameText;

            var musicTrack = _levelData.musicTrack;
            bottomLeftText.text = _levelData.name.IsNullOrEmpty()
                ? ""
                : $"\"{_levelData.name.ToUpper()}\" {(_levelData.author.IsNullOrEmpty() ? "" : "by " + _levelData.author.ToUpper())}";
            bottomRightText.text = musicTrack == MusicTrack.None ? "" : $"MUSIC: {musicTrack.Name.ToUpper()} by {musicTrack.Artist.ToUpper()}";
            bottomCanvasGroup.alpha = 0;

            yield return new WaitForSeconds(1);
            while (bottomCanvasGroup.alpha < 1) {
                bottomCanvasGroup.alpha += Time.fixedDeltaTime * 2;
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(3);

            while (bottomCanvasGroup.alpha > 0) {
                bottomCanvasGroup.alpha -= Time.fixedDeltaTime * 2;
                yield return new WaitForFixedUpdate();
            }

            bottomCanvasGroup.alpha = 0;
        }
    }
}