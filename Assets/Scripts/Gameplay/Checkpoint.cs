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
    private CheckpointType _type = CheckpointType.Check;

    public CheckpointType Type {
        get => _type;
        set {
            _type = value;
            Reset();
        }
    }

    [SerializeField] private MeshRenderer overlay;
    [SerializeField] private Material checkMaterial;
    [SerializeField] private Material validEndMaterial;
    [SerializeField] private Material invalidEndMaterial;
    
    private Track _track;

    private void OnEnable() {
        _track = GetComponentInParent<Track>();
    }

    public void Reset() {
        ShowOverlay();
        if (Type == CheckpointType.Start) {
            HideOverlay();
        }

        if (Type == CheckpointType.Check) {
            overlay.material = checkMaterial;
        }

        if (Type == CheckpointType.End) {
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
        if (Type == CheckpointType.Start) {
            return;
        }
        if (Type == CheckpointType.End && !_track.IsEndCheckpointValid) {
            return;
        }
        _track.CheckpointHit(this);
        HideOverlay();
    }

    public void Update() {
        if (Type == CheckpointType.End) {
            overlay.material = _track.IsEndCheckpointValid ? validEndMaterial : invalidEndMaterial;
        }
    }
}
