using System.Collections;
using System.Collections.Generic;
using System.IO;
using Core;
using Core.MapData;
using Core.Player;
using Core.Replays;
using Core.Scores;
using Core.ShipModel;
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
        private bool _isValid = true;

        // Handler refs
        [CanBeNull] private IGameModeWithScore _gameModeWithScore;
        [CanBeNull] private IGameModeWithCountdown _gameModeWithCountdown;
        [CanBeNull] private IGameModeWithCheckpoints _gameModeWithCheckpoint;
        [CanBeNull] private IGameModeWithTimer _gameModeWithTimer;

        // Concrete implementations
        private GameModeScore _gameModeScore;
        private readonly GameModeTimer _gameModeTimer = new();
        private GameModeLifecycle _gameModeLifecycle;
        private readonly GameModeCountdown _gameModeCountdown = new();

        // lifecycle
        private Coroutine _startSequenceCoroutine;

        private ShipPlayer LocalPlayer { get; set; }
        public bool HasStarted => LocalPlayer != null && LocalPlayer.ShipPhysics.ShipActive;

        private void OnEnable() {
            _replayRecorder = GetComponent<ReplayRecorder>();
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
        }

        private void OnDisable() {
            if (_track != null) _track.OnCheckpointHit -= OnCheckpointHit;
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
        }

        private void FixedUpdate() {
            if (HasStarted) {
                _gameModeTimer.Tick(Time.fixedDeltaTime);
                _gameMode.OnFixedUpdate();
            }
        }

        public void InitialiseGameMode(ShipPlayer localPlayer, LevelData levelData, IGameMode gameMode, InGameUI inGameUI, Track track) {
            CheckValidity();

            _startPosition = localPlayer.AbsoluteWorldPosition;
            _startRotation = localPlayer.transform.rotation;

            _gameModeScore = new GameModeScore(gameMode, levelData);
            _gameModeLifecycle = new GameModeLifecycle(this, localPlayer, Restart, Complete);

            LocalPlayer = localPlayer;
            _gameMode = gameMode;
            _inGameUI = inGameUI;
            _track = track;

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
        }

        public void Begin() {
            HandleStartSequence();
        }

        public void Restart() {
            StopGhosts();
            LocalPlayer.User.DisableUIInput();
            _inGameUI.GameModeUIHandler.GameModeUIText.HideGameUIText(false);
            _inGameUI.GameModeUIHandler.RaceResultsScreen.Hide();
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

            // nuke all ghosts and references
            StopGhosts();

            // clear out any old references
            _gameModeWithCountdown = null;
            _gameModeWithCheckpoint = null;
            _gameModeWithTimer = null;
        }

        private void OnCheckpointHit(Checkpoint checkpoint, float excessTimeToHitSeconds) {
            _gameModeWithCheckpoint?.OnCheckpointHit(checkpoint, excessTimeToHitSeconds);
        }

        private void OnGameSettingsApplied() {
            if (_gameMode != null)
                _gameMode.OnGameSettingsApplied();
            CheckValidity();
        }

        private void HandleStartSequence() {
            if (_startSequenceCoroutine != null) StopCoroutine(_startSequenceCoroutine);

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
                StartReplayRecordIfSupported();
                StartGhostsIfSupported();
                _gameModeScore.Reset();
                _gameMode.OnBegin();
                _inGameUI.GameModeUIHandler.GameModeUIText.ShowGameUIText();

                yield return StartCountdownIfRequired();

                // make absolutely double-decker sure the player couldn't get away before the timer said so
                LocalPlayer.SetTransformWorld(_startPosition, _startRotation);
                LocalPlayer.ShipPhysics.ResetPhysics(false);

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

        private void CheckValidity() {
            var version = Application.version;
            _isValid = _isValid && !version.Contains("-dev") && Game.Instance.ShipParameters.ToJsonString().Equals(ShipParameters.Defaults.ToJsonString());
        }
    }


    // // MADNESS I SAY
    // public class whocares : MonoBehaviour {
    //     // TODO: this crap is UI specific, move it to GameUI

    //
    //     public List<Checkpoint> hitCheckpoints;
    //
    //     private bool _complete;
    //     private IGameModeUI _gameModeUI;
    //
    //     // Set to true by the level loader but may be overridden for testing
    //     private bool _isActive;
    //     private bool _isValid = true;
    //
    //     private Score _previousBestScore;
    //     private ReplayRecorder _replayRecorder;
    //     private Replay _replayToRecord;
    //
    //     [CanBeNull] private Coroutine _splitDeltaFader;
    //     [CanBeNull] private Coroutine _splitFader;
    //     private List<float> _splits = new();
    //
    //     private float _timeSeconds;
    //
    //     protected float CurrentTimeSeconds { get; private set; }
    //     protected List<ShipGhost> ActiveGhosts { get; set; } = new();
    //
    //     public List<Checkpoint> Checkpoints {
    //         get => GetComponentsInChildren<Checkpoint>().ToList();
    //         set => ReplaceCheckpoints(value);
    //     }
    //
    //     private IGameModeUI GameModeUI {
    //         get {
    //             if (_gameModeUI == null) {
    //                 var ship = FdPlayer.FindLocalShipPlayer;
    //                 if (ship != null) _gameModeUI = ship.User.InGameUI.GameModeUIHandler.ActiveGameModeUI;
    //                 // OnBegin(); // TODO: WTF?! NO STAHP
    //             }
    //
    //             return _gameModeUI;
    //         }
    //     }
    //
    //     public void Begin() {
    //     }
    //
    //     public void Restart() {
    //     }
    //
    //     public void Complete() {
    //     }
    //
    //     public void Quit() {
    //     }
    //
    //     public void GameSettingsApplied() {
    //         CheckValidity();
    //         // OnGameSettingsApplied();
    //     }
    //
    //     #region INSANE
    //
    //     public bool IsEndCheckpointValid => hitCheckpoints.Count >= Checkpoints.Count - 2; // remove start and end
    //
    //     private void FixedUpdate() {
    //         // failing to get user in early stages due to modular loading? 
    //         if (_isActive && !_complete) {
    //             GameModeUI.Timers.TotalTimeDisplay.TextBox.color = WhiteTextColor;
    //             CurrentTimeSeconds += Time.fixedDeltaTime;
    //             GameModeUI.Timers.TotalTimeDisplay.SetTimeSeconds(Math.Abs(CurrentTimeSeconds));
    //         }
    //     }
    //
    //     private void OnEnable() {
    //         _replayRecorder = GetComponent<ReplayRecorder>();
    //         Game.OnRestart += Restart;
    //         Game.OnGameSettingsApplied += GameSettingsApplied;
    //     }
    //
    //     private void OnDisable() {
    //         Game.OnRestart -= Restart;
    //         Game.OnGameSettingsApplied -= GameSettingsApplied;
    //     }
    //
    //     private void ClearGhosts() {
    //         foreach (var shipGhost in ActiveGhosts) Game.Instance.RemoveGhost(shipGhost);
    //         ActiveGhosts = new List<ShipGhost>();
    //     }
    //
    //     // if the user ever changes parameters mid-level, the isValid flag is set to false and stays that way until restarting. It's also checked again
    //     // at the end of the race.
    //     private void CheckValidity() {
    //         var version = Application.version;
    //         _isValid = _isValid && !version.Contains("-dev") && Game.Instance.ShipParameters.ToJsonString().Equals(ShipParameters.Defaults.ToJsonString());
    //     }
    //
    //     public void InitialiseTrack() {
    //         _isValid = true;
    //         _previousBestScore = Score.ScoreForLevel(Game.Instance.LoadedLevelData);
    //
    //         if (Game.Instance.LoadedLevelData.gameType == GameType.Sprint) GameModeUI.Timers.ShowTimers();
    //
    //         var start = Checkpoints.Find(c => c.Type == CheckpointType.Start);
    //         if (start) {
    //             var ship = FdPlayer.FindLocalShipPlayer;
    //             if (ship != null) {
    //                 var startCheckpointTransform = start.transform;
    //                 ship.SetTransformLocal(startCheckpointTransform.position, startCheckpointTransform.rotation);
    //             }
    //         }
    //         else if (Checkpoints.Count > 0) {
    //             Debug.LogWarning("Checkpoints loaded with no start block! Is this intentional?");
    //         }
    //
    //         Checkpoints.ForEach(c => { c.Reset(); });
    //
    //         hitCheckpoints = new List<Checkpoint>();
    //         if (_isActive) {
    //             ResetTimer();
    //             StopTimer();
    //         }
    //     }
    //
    //     public void ResetTimer() {
    //         _complete = false;
    //         _timeSeconds = 0;
    //         _splits = new List<float>();
    //
    //         // reset timer text to 0, hide split timer
    //         if (GameModeUI != null) {
    //             GameModeUI.Timers.TotalTimeDisplay.SetTimeSeconds(0);
    //             GameModeUI.Timers.TotalTimeDisplay.TextBox.color = WhiteTextColor;
    //             GameModeUI.Timers.SplitTimeDisplay.TextBox.color = WhiteTextColor;
    //             GameModeUI.Timers.SplitTimeDeltaDisplay.TextBox.color = WhiteTextColor;
    //         }
    //     }
    //
    //     public void StopTimer() {
    //         _isActive = false;
    //         _complete = false;
    //     }
    //
    //
    //     public IEnumerator StartTrackWithCountdown() {
    //         if (Checkpoints.Count > 0) {
    //             _timeSeconds = -2.5f;
    //             _isActive = true;
    //             _complete = false;
    //
    //             // enable user input but disable actual movement
    //             var ship = FdPlayer.FindLocalShipPlayer;
    //             if (ship != null) {
    //                 var user = ship.User;
    //                 user.boostButtonForceEnabled = false;
    //                 user.EnableGameInput();
    //                 ship.Freeze = true;
    //
    //                 // Trigger recording and ghost replays
    //                 _replayRecorder.CancelRecording();
    //                 _replayRecorder.StartNewRecording(ship.ShipPhysics);
    //                 ClearGhosts();
    //                 foreach (var activeReplay in Game.Instance.ActiveGameReplays)
    //                     ActiveGhosts.Add(Game.Instance.LoadGhost(activeReplay));
    //
    //                 // half a second (2.5 second total) before countdown
    //                 yield return YieldExtensions.WaitForFixedFrames(YieldExtensions.SecondsToFixedFrames(0.5f));
    //
    //                 // start countdown sounds
    //                 UIAudioManager.Instance.Play("tt-countdown-1");
    //
    //                 // second beep (boost available here)
    //                 yield return YieldExtensions.WaitForFixedFrames(YieldExtensions.SecondsToFixedFrames(1));
    //                 UIAudioManager.Instance.Play("tt-countdown-1");
    //                 user.boostButtonForceEnabled = true;
    //
    //                 // GO! Unfreeze position and double-extra-special-make-sure the player is at the start
    //                 yield return YieldExtensions.WaitForFixedFrames(YieldExtensions.SecondsToFixedFrames(1));
    //                 UIAudioManager.Instance.Play("tt-countdown-2");
    //                 ship.Freeze = false;
    //                 var start = Checkpoints.Find(c => c.Type == CheckpointType.Start);
    //                 if (start) {
    //                     var startCheckpointTransform = start.transform;
    //                     ship.SetTransformLocal(startCheckpointTransform.position, startCheckpointTransform.rotation);
    //                     ship.ShipPhysics.ResetPhysics(false);
    //                 }
    //             }
    //         }
    //
    //         yield return new WaitForEndOfFrame();
    //     }
    //
    //     public void FinishTimer() {
    //         _complete = true;
    //     }
    //
    //     private IEnumerator FadeTimer(TimeDisplay timeDisplay, Color color) {
    //         timeDisplay.TextBox.color = color;
    //         while (timeDisplay.TextBox.color.a > 0.0f) {
    //             timeDisplay.TextBox.color = new Color(color.r, color.g, color.b,
    //                 timeDisplay.TextBox.color.a - Time.unscaledDeltaTime / 3);
    //             yield return null;
    //         }
    //     }
    //
    //     public void CheckpointHit(Checkpoint checkpoint, AudioSource checkpointHitAudio, float excessTimeToHitSeconds) {
    //         if (_isActive && GameModeUI?.Timers) {
    //             var hitCheckpoint = hitCheckpoints.Find(c => c == checkpoint);
    //             if (!hitCheckpoint && GameModeUI != null) {
    //                 // new checkpoint, record it and split timer
    //                 hitCheckpoints.Add(checkpoint);
    //                 checkpointHitAudio.Play();
    //
    //                 var exactTime = _timeSeconds + excessTimeToHitSeconds;
    //
    //                 // store split time
    //                 if (checkpoint.Type != CheckpointType.Start) {
    //                     _splits.Add(exactTime);
    //                     if (_splitDeltaFader != null) StopCoroutine(_splitDeltaFader);
    //                     if (_previousBestScore.HasPlayedPreviously && _previousBestScore.PersonalBestTimeSplits.Count >= _splits.Count) {
    //                         var index = _splits.Count - 1;
    //                         var previousBestSplit = _previousBestScore.PersonalBestTimeSplits[index];
    //                         var deltaSplit = exactTime - previousBestSplit;
    //                         GameModeUI.Timers.SplitTimeDeltaDisplay.SetTimeSeconds(deltaSplit, true);
    //                         var color = deltaSplit > 0 ? Color.red : Color.green;
    //                         GameModeUI.Timers.SplitTimeDeltaDisplay.TextBox.color = color;
    //                         _splitDeltaFader = StartCoroutine(FadeTimer(GameModeUI.Timers.SplitTimeDeltaDisplay, color));
    //                     }
    //                 }
    //
    //                 // update split display and fade out
    //                 if (checkpoint.Type == CheckpointType.Check) {
    //                     GameModeUI.Timers.SplitTimeDisplay.SetTimeSeconds(exactTime);
    //                     if (_splitFader != null) StopCoroutine(_splitFader);
    //                     _splitFader = StartCoroutine(FadeTimer(GameModeUI.Timers.SplitTimeDisplay, Color.white));
    //                 }
    //
    //                 if (checkpoint.Type == CheckpointType.End) {
    //                     if (_splitDeltaFader != null) StopCoroutine(_splitDeltaFader);
    //
    //                     var replayFileName = "";
    //                     var replayFilePath = "";
    //                     var score = Score.FromRaceTime(exactTime, _splits);
    //                     var previous = _previousBestScore;
    //
    //                     CheckValidity();
    //                     if (_isValid) {
    //                         // if new run OR better score, save!
    //                         // TODO: move this to the end screen too
    //                         if (_previousBestScore.PersonalBestTotalTime == 0 || exactTime < _previousBestScore.PersonalBestTotalTime) {
    //                             _previousBestScore = score;
    //                             var scoreData = score.Save(Game.Instance.LoadedLevelData);
    //                             Score.SaveToDisk(scoreData, Game.Instance.LoadedLevelData);
    //
    //                             if (_replayRecorder) {
    //                                 _replayRecorder.StopRecording();
    //                                 var levelHash = Game.Instance.LoadedLevelData.LevelHash();
    //                                 var replay = _replayRecorder.Replay;
    //                                 replayFileName = replay?.Save(scoreData);
    //                                 replayFilePath = Path.Combine(Replay.ReplayDirectory, levelHash, replayFileName ?? string.Empty);
    //                             }
    //                         }
    //
    //                         UpdateTargetTimeElements();
    //                     }
    //
    //                     GameModeUI.ShowResultsScreen(score, previous, _isValid, replayFileName, replayFilePath);
    //
    //                     FinishTimer();
    //                 }
    //             }
    //         }
    //     }
    //
    //
    //     private void ReplaceCheckpoints(List<Checkpoint> checkpoints) {
    //         foreach (var checkpoint in checkpoints) Destroy(checkpoint.gameObject);
    //
    //         hitCheckpoints = new List<Checkpoint>();
    //         InitialiseTrack();
    //     }
    //
    //     private void UpdateTargetTimeElements() {
    //         if (Game.Instance.LoadedLevelData.gameType.Id == GameType.Sprint.Id) {
    //             var levelData = Game.Instance.LoadedLevelData;
    //             var score = _previousBestScore;
    //
    //             var targetType = GameModeUI.Timers.TargetTimeTypeDisplay;
    //             var targetTimer = GameModeUI.Timers.TargetTimeDisplay;
    //             targetTimer.TextBox.color = Color.white;
    //
    //             var personalBest = score.PersonalBestTotalTime;
    //             var goldTargetTime = Score.GoldTimeTarget(levelData);
    //             var silverTargetTime = Score.SilverTimeTarget(levelData);
    //             var bronzeTargetTime = Score.BronzeTimeTarget(levelData);
    //
    //             // not played yet
    //             if (personalBest == 0) {
    //                 targetType.text = "TARGET BRONZE";
    //                 targetType.color = BronzeTextColor;
    //                 targetTimer.SetTimeSeconds(bronzeTargetTime);
    //                 return;
    //             }
    //
    //             if (personalBest < goldTargetTime) {
    //                 targetType.text = "PERSONAL BEST";
    //                 targetType.color = Color.white;
    //                 targetTimer.SetTimeSeconds(personalBest);
    //             }
    //             else if (personalBest < silverTargetTime) {
    //                 targetType.text = "TARGET GOLD";
    //                 targetType.color = GoldTextColor;
    //                 targetTimer.SetTimeSeconds(goldTargetTime);
    //             }
    //             else if (personalBest < bronzeTargetTime) {
    //                 targetType.text = "TARGET SILVER";
    //                 targetType.color = SilverTextColor;
    //                 targetTimer.SetTimeSeconds(silverTargetTime);
    //             }
    //             else {
    //                 targetType.text = "TARGET BRONZE";
    //                 targetType.color = BronzeTextColor;
    //                 targetTimer.SetTimeSeconds(bronzeTargetTime);
    //             }
    //         }
    //     }
    //
    //     #endregion
    // }
}