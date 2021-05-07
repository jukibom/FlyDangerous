using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CheckpointType {
    Start,
    Check,
    End,
}
public class Checkpoint : MonoBehaviour {
    public CheckpointType type = CheckpointType.Check;

    [SerializeField] private MeshRenderer overlay;
    [SerializeField] private Material checkMaterial;
    [SerializeField] private Material validEndMaterial;
    [SerializeField] private Material invalidEndMaterial;
    
    private Track _track;

    private void OnEnable() {
        _track = GetComponentInParent<Track>();
        if (type == CheckpointType.Start) {
            overlay.material = checkMaterial;
        }

        if (type == CheckpointType.Check) {
            HideOverlay();
        }

        if (type == CheckpointType.End) {
            overlay.material = invalidEndMaterial;
        }
    }

    public void ShowOverlay() {
        overlay.enabled = true;
    }
    
    public void HideOverlay() {
        overlay.enabled = false;
    }

    public void Hit() {
        if (type == CheckpointType.End && !_track.IsEndCheckpointValid) {
            return;
        }
        _track.CheckpointHit(this);
        HideOverlay();
    }

    public void Update() {
        if (type == CheckpointType.End) {
            overlay.material = _track.IsEndCheckpointValid ? validEndMaterial : invalidEndMaterial;
        }
    }
}
