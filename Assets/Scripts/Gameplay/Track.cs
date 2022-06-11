using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Audio;
using Core;
using Core.MapData;
using Core.Player;
using Core.Replays;
using Core.Scores;
using Core.ShipModel;
using Game_UI;
using GameUI.GameModes;
using JetBrains.Annotations;
using Misc;
using UnityEngine;

namespace Gameplay {
    [RequireComponent(typeof(ReplayRecorder))]
    public class Track : MonoBehaviour {
        private static readonly Color goldColor = new(1, 0.98f, 0.4f, 1);
        private static readonly Color silverColor = new(0.6f, 0.6f, 0.6f, 1);
        private static readonly Color bronzeColor = new(1, 0.43f, 0, 1);

        public List<Checkpoint> hitCheckpoints;

        // Set to true by the level loader but may be overridden for testing
        [SerializeField] private bool isActive;

        private bool _complete;
        private IGameModeUI _gameModeUI;

        private bool _isValid = true;

        private Score _previousBestScore;

        private ReplayRecorder _replayRecorder;
        private Replay _replayToRecord;
        [CanBeNull] private Coroutine _splitDeltaFader;
        [CanBeNull] private Coroutine _splitFader;
        private List<float> _splits = new();

        private float _timeSeconds;

        private IGameModeUI GameModeUI {
            get {
                if (_gameModeUI == null) {
                    var ship = FdPlayer.FindLocalShipPlayer;
                    if (ship != null) {
                        _gameModeUI = ship.User.InGameUI.GameModeUIHandler.ActiveGameModeUI;
                        UpdateTargetTimeElements();
                    }
                }

                return _gameModeUI;
            }
        }

        public List<Checkpoint> Checkpoints {
            get => GetComponentsInChildren<Checkpoint>().ToList();
            set => ReplaceCheckpoints(value);
        }

        public List<ShipGhost> ActiveGhosts { get; set; } = new();

        public bool IsEndCheckpointValid => hitCheckpoints.Count >= Checkpoints.Count - 2; // remove start and end

        private void FixedUpdate() {
            // failing to get user in early stages due to modular loading? 
            if (isActive && !_complete && GameModeUI != null && GameModeUI.Timers.TotalTimeDisplay != null) {
                GameModeUI.Timers.TotalTimeDisplay.TextBox.color = new Color(1f, 1f, 1f, 1f);
                _timeSeconds += Time.fixedDeltaTime;
                GameModeUI.Timers.TotalTimeDisplay.SetTimeSeconds(Math.Abs(_timeSeconds));
            }
        }

        private void OnEnable() {
            _replayRecorder = GetComponent<ReplayRecorder>();
            Game.OnRestart += InitialiseTrack;
            Game.OnGameSettingsApplied += CheckValidity;
        }

        private void OnDisable() {
            Game.OnRestart -= InitialiseTrack;
            Game.OnGameSettingsApplied -= CheckValidity;
        }

        public void InitialiseTrack() {
            _isValid = true;
            _previousBestScore = Score.ScoreForLevel(Game.Instance.LoadedLevelData);

            if (Game.Instance.LoadedLevelData.gameType == GameType.TimeTrial) GameModeUI.Timers.ShowTimers();

            var start = Checkpoints.Find(c => c.Type == CheckpointType.Start);
            if (start) {
                var ship = FdPlayer.FindLocalShipPlayer;
                if (ship != null) ship.transform.position = start.transform.position;
            }
            else if (Checkpoints.Count > 0) {
                Debug.LogWarning("Checkpoints loaded with no start block! Is this intentional?");
            }

            Checkpoints.ForEach(c => { c.Reset(); });

            hitCheckpoints = new List<Checkpoint>();
            if (isActive) {
                ResetTimer();
                StopTimer();
            }
        }

        public void ResetTimer() {
            _complete = false;
            _timeSeconds = 0;
            _splits = new List<float>();

            // reset timer text to 0, hide split timer
            if (GameModeUI != null) {
                GameModeUI.Timers.TotalTimeDisplay.SetTimeSeconds(0);
                GameModeUI.Timers.TotalTimeDisplay.TextBox.color = new Color(1f, 1f, 1f, 1);
                GameModeUI.Timers.SplitTimeDisplay.TextBox.color = new Color(1f, 1f, 1f, 0);
                GameModeUI.Timers.SplitTimeDeltaDisplay.TextBox.color = new Color(1f, 1f, 1f, 0);
            }
        }

        public void StopTimer() {
            isActive = false;
            _complete = false;
        }

        public void ClearGhosts() {
            foreach (var shipGhost in ActiveGhosts) Game.Instance.RemoveGhost(shipGhost);
            ActiveGhosts = new List<ShipGhost>();
        }

        public IEnumerator StartTrackWithCountdown() {
            if (Checkpoints.Count > 0) {
                _timeSeconds = -2.5f;
                isActive = true;
                _complete = false;

                // enable user input but disable actual movement
                var player = FdPlayer.FindLocalShipPlayer;
                if (player != null) {
                    var user = player.User;
                    user.boostButtonForceEnabled = false;
                    user.EnableGameInput();
                    player.Freeze = true;


                    // Trigger recording and ghost replays
                    _replayRecorder.CancelRecording();
                    _replayRecorder.StartNewRecording(player.ShipPhysics);
                    ClearGhosts();
                    foreach (var activeReplay in Game.Instance.ActiveGameReplays)
                        ActiveGhosts.Add(Game.Instance.LoadGhost(activeReplay));

                    // half a second (2.5 second total) before countdown
                    yield return YieldExtensions.WaitForFixedFrames(YieldExtensions.SecondsToFixedFrames(0.5f));

                    // start countdown sounds
                    UIAudioManager.Instance.Play("tt-countdown-1");

                    // second beep (boost available here)
                    yield return YieldExtensions.WaitForFixedFrames(YieldExtensions.SecondsToFixedFrames(1));
                    UIAudioManager.Instance.Play("tt-countdown-1");
                    user.boostButtonForceEnabled = true;

                    // GO! Unfreeze position and double-extra-special-make-sure the player is at the start
                    yield return YieldExtensions.WaitForFixedFrames(YieldExtensions.SecondsToFixedFrames(1));
                    UIAudioManager.Instance.Play("tt-countdown-2");
                    player.Freeze = false;
                    var start = Checkpoints.Find(c => c.Type == CheckpointType.Start);
                    if (start) {
                        player.transform.position = start.transform.position;
                        player.ShipPhysics.ResetPhysics(false);
                    }
                }
            }

            yield return new WaitForEndOfFrame();
        }

        public void FinishTimer() {
            _complete = true;
        }

        private IEnumerator FadeTimer(TimeDisplay timeDisplay, Color color) {
            timeDisplay.TextBox.color = color;
            while (timeDisplay.TextBox.color.a > 0.0f) {
                timeDisplay.TextBox.color = new Color(color.r, color.g, color.b,
                    timeDisplay.TextBox.color.a - Time.unscaledDeltaTime / 3);
                yield return null;
            }
        }

        public void CheckpointHit(Checkpoint checkpoint, AudioSource checkpointHitAudio, float excessTimeToHitSeconds) {
            if (isActive && GameModeUI?.Timers) {
                var hitCheckpoint = hitCheckpoints.Find(c => c == checkpoint);
                if (!hitCheckpoint && GameModeUI != null) {
                    // new checkpoint, record it and split timer
                    hitCheckpoints.Add(checkpoint);
                    checkpointHitAudio.Play();

                    var exactTime = _timeSeconds + excessTimeToHitSeconds;

                    // store split time
                    if (checkpoint.Type != CheckpointType.Start) {
                        _splits.Add(exactTime);
                        if (_splitDeltaFader != null) StopCoroutine(_splitDeltaFader);
                        if (_previousBestScore.HasPlayedPreviously && _previousBestScore.PersonalBestTimeSplits.Count >= _splits.Count) {
                            var index = _splits.Count - 1;
                            var previousBestSplit = _previousBestScore.PersonalBestTimeSplits[index];
                            var deltaSplit = exactTime - previousBestSplit;
                            GameModeUI.Timers.SplitTimeDeltaDisplay.SetTimeSeconds(deltaSplit, true);
                            var color = deltaSplit > 0 ? Color.red : Color.green;
                            GameModeUI.Timers.SplitTimeDeltaDisplay.TextBox.color = color;
                            _splitDeltaFader = StartCoroutine(FadeTimer(GameModeUI.Timers.SplitTimeDeltaDisplay, color));
                        }
                    }

                    // update split display and fade out
                    if (checkpoint.Type == CheckpointType.Check) {
                        GameModeUI.Timers.SplitTimeDisplay.SetTimeSeconds(exactTime);
                        if (_splitFader != null) StopCoroutine(_splitFader);
                        _splitFader = StartCoroutine(FadeTimer(GameModeUI.Timers.SplitTimeDisplay, Color.white));
                    }

                    if (checkpoint.Type == CheckpointType.End) {
                        if (_splitDeltaFader != null) StopCoroutine(_splitDeltaFader);

                        var replayFileName = "";
                        var replayFilePath = "";
                        var score = Score.FromRaceTime(exactTime, _splits);
                        var previous = _previousBestScore;

                        CheckValidity();
                        if (_isValid) {
                            // if new run OR better score, save!
                            // TODO: move this to the end screen too
                            if (_previousBestScore.PersonalBestTotalTime == 0 || exactTime < _previousBestScore.PersonalBestTotalTime) {
                                _previousBestScore = score;
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

                            UpdateTargetTimeElements();
                        }

                        GameModeUI.ShowResultsScreen(score, previous, _isValid, replayFileName, replayFilePath);

                        FinishTimer();
                    }
                }
            }
        }

        // if the user ever changes parameters mid-level, the isValid flag is set to false and stays that way until restarting. It's also checked again
        // at the end of the race.
        private void CheckValidity() {
            var version = Application.version;
            _isValid = _isValid && !version.Contains("-dev") && Game.Instance.ShipParameters.ToJsonString().Equals(ShipParameters.Defaults.ToJsonString());
        }

        private void ReplaceCheckpoints(List<Checkpoint> checkpoints) {
            foreach (var checkpoint in checkpoints) Destroy(checkpoint.gameObject);

            hitCheckpoints = new List<Checkpoint>();
            InitialiseTrack();
        }

        private void UpdateTargetTimeElements() {
            if (Game.Instance.LoadedLevelData.gameType.Id == GameType.TimeTrial.Id) {
                var levelData = Game.Instance.LoadedLevelData;
                var score = _previousBestScore;

                var targetType = GameModeUI.Timers.TargetTimeTypeDisplay;
                var targetTimer = GameModeUI.Timers.TargetTimeDisplay;
                targetTimer.TextBox.color = Color.white;

                var personalBest = score.PersonalBestTotalTime;
                var goldTargetTime = Score.GoldTimeTarget(levelData);
                var silverTargetTime = Score.SilverTimeTarget(levelData);
                var bronzeTargetTime = Score.BronzeTimeTarget(levelData);

                // not played yet
                if (personalBest == 0) {
                    targetType.text = "TARGET BRONZE";
                    targetType.color = bronzeColor;
                    targetTimer.SetTimeSeconds(bronzeTargetTime);
                    return;
                }

                if (personalBest < goldTargetTime) {
                    targetType.text = "PERSONAL BEST";
                    targetType.color = Color.white;
                    targetTimer.SetTimeSeconds(personalBest);
                }
                else if (personalBest < silverTargetTime) {
                    targetType.text = "TARGET GOLD";
                    targetType.color = goldColor;
                    targetTimer.SetTimeSeconds(goldTargetTime);
                }
                else if (personalBest < bronzeTargetTime) {
                    targetType.text = "TARGET SILVER";
                    targetType.color = silverColor;
                    targetTimer.SetTimeSeconds(silverTargetTime);
                }
                else {
                    targetType.text = "TARGET BRONZE";
                    targetType.color = bronzeColor;
                    targetTimer.SetTimeSeconds(bronzeTargetTime);
                }
            }
        }
    }
}