using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Core;
using Core.MapData;
using Core.Player;
using Core.Scores;
using Core.ShipModel;
using Game_UI;
using GameUI;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay {
    public class Track : MonoBehaviour {
        private static readonly Color goldColor = new(1, 0.98f, 0.4f, 1);
        private static readonly Color silverColor = new(0.6f, 0.6f, 0.6f, 1);
        private static readonly Color bronzeColor = new(1, 0.43f, 0, 1);

        public List<Checkpoint> hitCheckpoints;

        // Set to true by the level loader but may be overridden for testing
        [SerializeField] private bool isActive;
        private bool _complete;

        private Score _previousBestScore;
        [CanBeNull] private Coroutine _splitDeltaFader;
        [CanBeNull] private Coroutine _splitFader;
        private List<float> _splits;
        private Timers _timers;

        private float _timeSeconds;

        // DONT use in Start or OnEnable
        private Timers Timers {
            get {
                if (_timers == null) {
                    var ship = FdPlayer.FindLocalShipPlayer;
                    if (ship) {
                        _timers = ship.User.InGameUI.Timers;
                        UpdateTargetTimeElements();
                    }
                }

                return _timers;
            }
        }

        public List<Checkpoint> Checkpoints {
            get => GetComponentsInChildren<Checkpoint>().ToList();
            set => ReplaceCheckpoints(value);
        }

        public bool IsEndCheckpointValid => hitCheckpoints.Count >= Checkpoints.Count - 2; // remove start and end

        private void FixedUpdate() {
            // failing to get user in early stages due to modular loading? 
            if (isActive && !_complete && Timers != null && Timers.TotalTimeDisplay != null) {
                Timers.TotalTimeDisplay.TextBox.color = new Color(1f, 1f, 1f, 1f);
                _timeSeconds += Time.fixedDeltaTime;
                Timers.TotalTimeDisplay.SetTimeSeconds(Math.Abs(_timeSeconds));
            }
        }

        private void OnEnable() {
            Game.OnRestart += InitialiseTrack;
        }

        private void OnDisable() {
            Game.OnRestart -= InitialiseTrack;
        }

        public void InitialiseTrack() {
            _previousBestScore = Score.ScoreForLevel(Game.Instance.LoadedLevelData);

            Timers.HideTimers(false);
            if (Game.Instance.LoadedLevelData.gameType.Name == GameType.TimeTrial.Name) Timers.ShowTimers();

            var start = Checkpoints.Find(c => c.Type == CheckpointType.Start);
            if (start) {
                var ship = FdPlayer.FindLocalShipPlayer;
                if (ship) ship.transform.position = start.transform.position;
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
            if (Timers) {
                Timers.TotalTimeDisplay.SetTimeSeconds(0);
                Timers.TotalTimeDisplay.TextBox.color = new Color(1f, 1f, 1f, 1);
                Timers.SplitTimeDisplay.TextBox.color = new Color(1f, 1f, 1f, 0);
                Timers.SplitTimeDeltaDisplay.TextBox.color = new Color(1f, 1f, 1f, 0);
            }
        }

        public void StopTimer() {
            isActive = false;
            _complete = false;
        }

        public IEnumerator StartTrackWithCountdown() {
            if (Checkpoints.Count > 0) {
                ResetTimer();
                _timeSeconds = -2.5f;
                isActive = true;
                _complete = false;

                // enable user input but disable actual movement
                var player = FdPlayer.FindLocalShipPlayer;
                if (player != null) {
                    var user = player.User;
                    user.EnableGameInput();
                    user.movementEnabled = false;
                    user.pauseMenuEnabled = false;

                    // half a second (2.5 second total) before countdown
                    yield return new WaitForSeconds(0.5f);

                    // start countdown sounds
                    UIAudioManager.Instance.Play("tt-countdown");

                    // second beep (boost available here)
                    yield return new WaitForSeconds(1);
                    user.boostButtonEnabledOverride = true;

                    // GO!
                    yield return new WaitForSeconds(1);
                    user.movementEnabled = true;
                    user.pauseMenuEnabled = true;
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

        public void CheckpointHit(Checkpoint checkpoint, AudioSource checkpointHitAudio) {
            if (isActive && Timers) {
                var hitCheckpoint = hitCheckpoints.Find(c => c == checkpoint);
                if (!hitCheckpoint) {
                    // new checkpoint, record it and split timer
                    hitCheckpoints.Add(checkpoint);
                    checkpointHitAudio.Play();

                    // store split time
                    if (checkpoint.Type != CheckpointType.Start) {
                        _splits.Add(_timeSeconds);
                        if (_splitDeltaFader != null) StopCoroutine(_splitDeltaFader);
                        if (_previousBestScore.HasPlayedPreviously && _previousBestScore.PersonalBestTimeSplits.Count >= _splits.Count) {
                            var index = _splits.Count - 1;
                            var previousBestSplit = _previousBestScore.PersonalBestTimeSplits[index];
                            var deltaSplit = _timeSeconds - previousBestSplit;
                            Timers.SplitTimeDeltaDisplay.SetTimeSeconds(deltaSplit, true);
                            var color = deltaSplit > 0 ? Color.red : Color.green;
                            Timers.SplitTimeDeltaDisplay.TextBox.color = color;
                            _splitDeltaFader = StartCoroutine(FadeTimer(Timers.SplitTimeDeltaDisplay, color));
                        }
                    }

                    // update split display and fade out
                    if (checkpoint.Type == CheckpointType.Check) {
                        Timers.SplitTimeDisplay.SetTimeSeconds(_timeSeconds);
                        if (_splitFader != null) StopCoroutine(_splitFader);
                        _splitFader = StartCoroutine(FadeTimer(Timers.SplitTimeDisplay, Color.white));
                    }

                    if (checkpoint.Type == CheckpointType.End) {
                        if (_splitDeltaFader != null) StopCoroutine(_splitDeltaFader);

                        // TODO: Make this more generalised and implement a tinker tier for saving these times
                        if (!FindObjectOfType<Game>().ShipParameters.ToJsonString()
                                .Equals(ShipParameters.Defaults.ToJsonString())) {
                            // you dirty debug cheater!
                            Timers.TotalTimeDisplay.GetComponent<Text>().color = new Color(1, 1, 0, 1);
                        }

                        else {
                            Timers.TotalTimeDisplay.GetComponent<Text>().color = new Color(0, 1, 0, 1);
                            // if new run OR better score, save!
                            if (_previousBestScore.PersonalBestTotalTime == 0 || _timeSeconds < _previousBestScore.PersonalBestTotalTime) {
                                var score = Score.NewPersonalBest(Game.Instance.LoadedLevelData, _timeSeconds, _splits);
                                _previousBestScore = score;
                                score.Save();
                            }

                            UpdateTargetTimeElements();
                        }

                        FinishTimer();
                        // TODO: End screen
                    }
                }
            }
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

                var targetType = Timers.TargetTimeTypeDisplay;
                var targetTimer = Timers.TargetTimeDisplay;
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