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
        private enum TimeTrialCheckpointMode {
            Start,
            End,
            Both
        }

        private TimeTrialCheckpointMode _timeTrialCheckpointMode;

        private Checkpoint _startCheckpoint;
        private Checkpoint _endCheckpoint;
        private int _lap = 1;
        private readonly List<float> _lapSplits = new();
        private const int TotalLaps = 3;

        public override void OnInitialise() {
            _startCheckpoint = GameModeCheckpoints.Checkpoints.Find(c => c.Type == CheckpointType.Start);
            _endCheckpoint = GameModeCheckpoints.Checkpoints.Find(c => c.Type == CheckpointType.Start);

            if (_startCheckpoint != null) _timeTrialCheckpointMode = TimeTrialCheckpointMode.Start;
            if (_endCheckpoint != null) _timeTrialCheckpointMode = TimeTrialCheckpointMode.End;
            if (_startCheckpoint != null && _endCheckpoint != null) _timeTrialCheckpointMode = TimeTrialCheckpointMode.Both;

            if (_timeTrialCheckpointMode == TimeTrialCheckpointMode.End)
                _endCheckpoint.Type = CheckpointType.Start;
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();
            var lapTimesToDisplay = new List<float>(_lapSplits);
            lapTimesToDisplay.Add(GameModeTimer.CurrentSessionTimeSeconds);
            UpdateLapCounterUI(lapTimesToDisplay);
        }

        public override void OnCheckpointHit(Checkpoint checkpoint, float excessTimeToHitMs) {
            if (_timeTrialCheckpointMode == TimeTrialCheckpointMode.Start) _startCheckpoint.Type = CheckpointType.End;
            if (checkpoint.Type == CheckpointType.Check) _endCheckpoint.Reset();

            base.OnCheckpointHit(checkpoint, excessTimeToHitMs);
        }

        protected override void LastCheckpointHit(float hitAtTime) {
            _lapSplits.Add(hitAtTime);
            if (_lap < TotalLaps) {
                GameModeCheckpoints.Reset();
                _lap++;
            }
            else {
                GameModeLifecycle.Complete();
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