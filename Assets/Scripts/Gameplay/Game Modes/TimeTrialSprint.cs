using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Gameplay.Game_Modes.Components.Interfaces;
using GameUI.GameModes;
using Misc;
using UnityEngine;

namespace Gameplay.Game_Modes {
    public class TimeTrialSprint : IGameMode, IGameModeWithScore, IGameModeWithCountdown, IGameModeWithTimer, IGameModeWithCheckpoints {
        // These are set by the game mode handler by virtue of implementing the interface
        public GameModeUIHandler GameModeUIHandler { get; set; }
        public IGameModeScore GameModeScore { get; set; }
        public IGameModeLifecycle GameModeLifecycle { get; set; }
        public IGameModeCheckpoints GameModeCheckpoints { get; set; }
        public IGameModeTimer GameModeTimer { get; set; }

        // Exposed game mode metadata
        public bool IsHotJoinable => false;
        public bool CanWarpToHost => false;
        public bool HasFixedStartLocation => true;
        public bool IsStartLocationAlwaysPreLoaded => true;

        public bool SupportsReplays => true;
        public bool RequireBoostHeldToStart => true;
        public GameModeScoreType GameModeScoreType => GameModeScoreType.Time;
        public bool AllowEarlyBoost => true;
        public float StartingCountdownTime => 2.5f;


        protected readonly List<float> _splits = new();
        protected float _lastCheckpointHitTimeSeconds;
        protected Coroutine _splitFadeOutCoroutine;
        protected Coroutine _startTextCoroutine;

        public virtual void OnInitialise() {
            _splits.Clear();
            _lastCheckpointHitTimeSeconds = 0;
        }

        public virtual void OnBegin() {
            GameModeUIHandler.GameModeUIText.TopCanvasGroup.alpha = 1;
            GameModeUIHandler.GameModeUIText.RightCanvasGroup.alpha = 1;
            GameModeUIHandler.GameModeUIText.CentralNotificationCanvasGroup.alpha = 1;
            if (_startTextCoroutine != null) Game.Instance.StopCoroutine(_startTextCoroutine);
            _startTextCoroutine = Game.Instance.StartCoroutine(ShowStarterText());
        }

        public virtual void OnFixedUpdate() {
            // display 00:00:00 until it starts
            var timerDisplay = Math.Max(0, GameModeTimer.CurrentSessionTimeSeconds);
            GameModeUIHandler.GameModeUIText.TopHeader.text = TimeExtensions.TimeSecondsToString(Mathf.Abs(timerDisplay));
        }

        public virtual void OnRestart() {
            if (_splitFadeOutCoroutine != null) Game.Instance.StopCoroutine(_splitFadeOutCoroutine);
            if (_startTextCoroutine != null) Game.Instance.StopCoroutine(_startTextCoroutine);

            GameModeUIHandler.GameModeUIText.TopCanvasGroup.alpha = 0;
            GameModeUIHandler.GameModeUIText.RightCanvasGroup.alpha = 0;
            GameModeUIHandler.GameModeUIText.CentralCanvasGroup.alpha = 0;
            GameModeUIHandler.GameModeUIText.CentralNotificationCanvasGroup.alpha = 0;
            GameModeCheckpoints.Reset();
            _splits.Clear();
            _lastCheckpointHitTimeSeconds = 0;
        }

        public virtual void OnComplete() {
            GameModeUIHandler.GameModeUIText.HideGameUIText(false);
            GameModeScore.NewScore(_lastCheckpointHitTimeSeconds, _splits);
        }

        public virtual void OnQuit() {
            if (_splitFadeOutCoroutine != null) Game.Instance.StopCoroutine(_splitFadeOutCoroutine);
            if (_startTextCoroutine != null) Game.Instance.StopCoroutine(_startTextCoroutine);
        }

        public virtual void CountdownStarted() {
            /* nothing to do */
        }

        public virtual void CountdownComplete() {
            /* nothing to do */
        }

        public virtual void OnGameSettingsApplied() {
            /* nothing to do */
        }

        public virtual void OnCheckpointHit(Checkpoint checkpoint, float hitTimeSeconds) {
            _lastCheckpointHitTimeSeconds = hitTimeSeconds;

            // store split 
            _splits.Add(_lastCheckpointHitTimeSeconds);
            float previousSplitTimeSeconds = 0;
            if (GameModeScore.CurrentBestSplits.Count >= _splits.Count) previousSplitTimeSeconds = GameModeScore.CurrentBestSplits[_splits.Count - 1];
            SetSplitTimer(_lastCheckpointHitTimeSeconds, previousSplitTimeSeconds);

            // enable last checkpoint and complete
            if (GameModeCheckpoints.AllCheckpointsHit) {
                // enable the end checkpoint
                var endCheckpoint = GameModeCheckpoints.Checkpoints.Find(c => c.Type == CheckpointType.End);
                if (endCheckpoint) endCheckpoint.ToggleValidEndMaterial(true);
            }
        }

        public virtual void OnLastCheckpointHit(float hitTimeSeconds) {
            if (GameModeCheckpoints.AllCheckpointsHit)
                GameModeLifecycle.Complete();
        }

        public void SetCurrentPersonalBest(float score, List<float> splits) {
            var targetScore = GameModeScore.NextTargetScore();
            GameModeUIHandler.GameModeUIText.TopRightHeader.color = targetScore.MedalColor;
            GameModeUIHandler.GameModeUIText.TopRightHeader.text = $"TARGET {targetScore.Medal}".ToUpper();
            GameModeUIHandler.GameModeUIText.TopRightContent.text = TimeExtensions.TimeSecondsToString(targetScore.Score);
        }


        protected void SetSplitTimer(float splitTimeSeconds, float previousSplitTimeSeconds = 0) {
            GameModeUIHandler.GameModeUIText.CentralHeader.color = Color.white;
            GameModeUIHandler.GameModeUIText.CentralHeader.text = TimeExtensions.TimeSecondsToString(splitTimeSeconds);
            if (previousSplitTimeSeconds > 0) {
                var deltaSplit = splitTimeSeconds - previousSplitTimeSeconds;
                var deltaSplitText = TimeExtensions.TimeSecondsToString(deltaSplit);
                GameModeUIHandler.GameModeUIText.CentralContent.color = deltaSplit > 0 ? Color.red : Color.green;
                GameModeUIHandler.GameModeUIText.CentralContent.text = deltaSplit > 0 ? $"+{deltaSplitText}" : deltaSplitText;
            }

            ShowAndFadeOutSplits();
        }

        protected void ShowAndFadeOutSplits() {
            if (_splitFadeOutCoroutine != null) Game.Instance.StopCoroutine(_splitFadeOutCoroutine);
            GameModeUIHandler.GameModeUIText.CentralCanvasGroup.alpha = 1;
            _splitFadeOutCoroutine = Game.Instance.StartCoroutine(
                YieldExtensions.SimpleAnimationTween(
                    val => GameModeUIHandler.GameModeUIText.CentralCanvasGroup.alpha = 1 - val,
                    5f
                )
            );
        }

        protected IEnumerator ShowStarterText() {
            while (GameModeTimer.CurrentSessionTimeSeconds < 2.5f) {
                var time = GameModeTimer.CurrentSessionTimeSeconds;

                var starterTextColor = time < -1 ? Color.Lerp(Color.red, Color.white, time.Remap(-1.5f, -1f, 0, 1))
                    : time < 0 ? Color.Lerp(Color.yellow, Color.white, time.Remap(-0.5f, 0, 0, 1))
                    : time < 1.5f ? Color.Lerp(Color.green, Color.white, time.Remap(1, 1.5f, 0, 1)) : Color.white;

                var starterText = time < -2 ? "" :
                    time < -1 ? "READY" :
                    time < 0 ? "SET" :
                    "GO!";

                GameModeUIHandler.GameModeUIText.CentralNotification.color = starterTextColor;
                GameModeUIHandler.GameModeUIText.CentralNotification.text = starterText;

                // get our current time for each second as value between 0 and 1 for purpose of font size animation fun times
                var fontScaleFromTime = time < -1 ? 2 + time :
                    time < 0 ? 1 + time :
                    0; // on "GO" just stay at max scale

                var scale = fontScaleFromTime.Remap(0.5f, 1f, 1f, 0.6f);
                GameModeUIHandler.GameModeUIText.CentralNotification.transform.localScale = new Vector3(scale, scale, scale);

                // fade out from 1.5s to 2.5s at start
                GameModeUIHandler.GameModeUIText.CentralNotificationCanvasGroup.alpha = time.Remap(1.5f, 2f, 1, 0);
                yield return new WaitForFixedUpdate();
            }

            // reset notification state for whatever future uses
            GameModeUIHandler.GameModeUIText.CentralNotification.transform.localScale = Vector3.one;
            GameModeUIHandler.GameModeUIText.CentralNotificationCanvasGroup.alpha = 0;
        }
    }
}