using Gameplay;
using UnityEngine;

public enum CheckpointType {
    Start,
    Check,
    End
}

public class Checkpoint : MonoBehaviour {
    [SerializeField] private CheckpointType type = CheckpointType.Check;

    [SerializeField] private MeshRenderer overlay;
    [SerializeField] private Material checkMaterial;
    [SerializeField] private Material validEndMaterial;
    [SerializeField] private Material invalidEndMaterial;
    [SerializeField] private AudioSource checkpointAudioSource;

    private Track _track;

    public CheckpointType Type {
        get => type;
        set {
            type = value;
            Reset();
        }
    }

    public void Reset() {
        ShowOverlay();
        if (Type == CheckpointType.Start) HideOverlay();

        if (Type == CheckpointType.Check) overlay.material = checkMaterial;

        if (Type == CheckpointType.End) overlay.material = invalidEndMaterial;
    }

    public void Update() {
        if (Type == CheckpointType.End) overlay.material = _track.IsEndCheckpointValid ? validEndMaterial : invalidEndMaterial;
    }

    private void OnEnable() {
        _track = GetComponentInParent<Track>();
    }

    public void ShowOverlay() {
        overlay.gameObject.SetActive(true);
    }

    public void HideOverlay() {
        overlay.gameObject.SetActive(false);
    }

    public void Hit(float excessTimeToHitMs) {
        if (Type == CheckpointType.Start) return;
        if (Type == CheckpointType.End && !_track.IsEndCheckpointValid) return;
        _track.CheckpointHit(this, checkpointAudioSource, excessTimeToHitMs);
        HideOverlay();
    }
}