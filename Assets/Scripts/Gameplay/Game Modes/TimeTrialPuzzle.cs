using System.Linq;

namespace Gameplay.Game_Modes {
    // This class is essentially just time trial sprint with some minor alterations:
    // Start checkpoint is ignored / removed, End checkpoint is replaced with a regular checkpoint.
    // No defined path so on checkpoint hit we check to see if it was the last one and that's the trigger to end.
    public class TimeTrialPuzzle : TimeTrialSprint {
        public override void OnInitialise() {
            GameModeCheckpoints.Checkpoints.ForEach(checkpoint => {
                if (checkpoint.Type == CheckpointType.Start) checkpoint.gameObject.SetActive(false);
                if (checkpoint.Type == CheckpointType.End) checkpoint.Type = CheckpointType.Check;
            });
        }

        public override void OnBegin() {
            base.OnBegin();
            GameModeUIHandler.GameModeUIText.LeftCanvasGroup.alpha = 1;
            UpdateCheckpointCounterUI();
        }

        public override void OnCheckpointHit(Checkpoint checkpoint, float hitTimeSeconds) {
            base.OnCheckpointHit(checkpoint, hitTimeSeconds);
            UpdateCheckpointCounterUI();
            if (GameModeCheckpoints.AllCheckpointsHit) OnLastCheckpointHit(hitTimeSeconds);
        }

        protected void UpdateCheckpointCounterUI() {
            var checkpointsHit = GameModeCheckpoints.Checkpoints
                .FindAll(checkpoint => checkpoint.IsHit)
                .Count;
            var checkpointsTotal = GameModeCheckpoints.Checkpoints.Select(checkpoint => checkpoint.Type == CheckpointType.Check).Count();

            GameModeUIHandler.GameModeUIText.TopLeftHeader.text = "CHECKPOINTS HIT";
            GameModeUIHandler.GameModeUIText.TopLeftContent.text = $"{checkpointsHit} / {checkpointsTotal}";
        }
    }
}