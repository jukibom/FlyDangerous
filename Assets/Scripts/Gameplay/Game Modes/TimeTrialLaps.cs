using System.Collections.Generic;
using System.Linq;
using Misc;

namespace Gameplay.Game_Modes {
    // This class is essentially just time trial sprint with some minor alterations:
    // Keep track of laps, on completion we just increment that count and reset the course.
    // Logic for checkpoints is:
    // Search for either a start or an end - if we have both, we presume that start is unique to the first lap.
    // If we find start, we use that until the first checkpoint is hit and then it becomes the end.
    // If we find end, that becomes start and the previous logic applies.
    public class TimeTrialLaps : TimeTrialSprint {
        private enum TimeTrialLapsCheckpointMode {
            Start,
            End,
            Both
        }

        private TimeTrialLapsCheckpointMode _timeTrialLapsCheckpointMode;

        private Checkpoint _startCheckpoint;
        private Checkpoint _endCheckpoint;
        private int _lap = 1;
        private float _timeAtPreviousLapCycle;
        private readonly List<float> _lapSplits = new();
        private const int TotalLaps = 3;

        public override void OnInitialise() {
            base.OnInitialise();
            _lap = 1;
            _timeAtPreviousLapCycle = 0;
            _lapSplits.Clear();

            _startCheckpoint = GameModeCheckpoints.Checkpoints.Find(c => c.Type == CheckpointType.Start);
            _endCheckpoint = GameModeCheckpoints.Checkpoints.Find(c => c.Type == CheckpointType.End);

            if (_startCheckpoint != null) _timeTrialLapsCheckpointMode = TimeTrialLapsCheckpointMode.Start;
            if (_endCheckpoint != null) _timeTrialLapsCheckpointMode = TimeTrialLapsCheckpointMode.End;
            if (_startCheckpoint != null && _endCheckpoint != null) _timeTrialLapsCheckpointMode = TimeTrialLapsCheckpointMode.Both;

            if (_timeTrialLapsCheckpointMode == TimeTrialLapsCheckpointMode.End) {
                _startCheckpoint = _endCheckpoint;
                _startCheckpoint.Type = CheckpointType.Start;
                _timeTrialLapsCheckpointMode = TimeTrialLapsCheckpointMode.Start;
            }
        }

        public override void OnBegin() {
            base.OnBegin();
            GameModeUIHandler.GameModeUIText.LeftCanvasGroup.alpha = 1;
        }

        public override void OnRestart() {
            OnInitialise();
            base.OnRestart();
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();

            // display lap 1 as 00:00 until we're actually ready to count up
            List<float> lapTimesToDisplay;
            if (!GameModeLifecycle.HasStarted)
                lapTimesToDisplay = new List<float> { 0 };
            else
                lapTimesToDisplay = new List<float>(_lapSplits) { GameModeTimer.CurrentSessionTimeSeconds - _timeAtPreviousLapCycle };

            UpdateLapCounterUI(lapTimesToDisplay);
        }

        public override void OnCheckpointHit(Checkpoint checkpoint, float hitTimeSeconds) {
            // if we only have a starting checkpoint, swap it to end after the first hit
            if (_timeTrialLapsCheckpointMode == TimeTrialLapsCheckpointMode.Start) {
                _startCheckpoint.Type = CheckpointType.End;
                _endCheckpoint = _startCheckpoint;
            }

            if (checkpoint.Type == CheckpointType.Check) _endCheckpoint.Reset();

            base.OnCheckpointHit(checkpoint, hitTimeSeconds - _timeAtPreviousLapCycle);
        }

        public override void OnLastCheckpointHit(float hitTimeSeconds) {
            if (GameModeCheckpoints.AllCheckpointsHit) {
                // store the lap time (current game time minus time at last lap) and store new time
                _lapSplits.Add(hitTimeSeconds - _timeAtPreviousLapCycle);
                _timeAtPreviousLapCycle = hitTimeSeconds;

                // reset or complete depending on lap count
                if (_lap < TotalLaps) {
                    GameModeCheckpoints.Reset();
                    _lap++;
                }
                else {
                    // override last checkpoint hit time for the purpose of storing a final score
                    _lastCheckpointHitTimeSeconds = hitTimeSeconds;
                    GameModeLifecycle.Complete();
                }
            }
        }

        protected void UpdateLapCounterUI(List<float> timesToDisplay) {
            GameModeUIHandler.GameModeUIText.TopLeftHeader.text = "LAPS";
            GameModeUIHandler.GameModeUIText.TopLeftContent.text = timesToDisplay
                .ConvertAll(TimeExtensions.TimeSecondsToString)
                .Select((time, index) => new { time, index })
                .Aggregate("", (acc, cur) => $"{acc}{cur.index + 1}: {cur.time}\n");
        }
    }
}