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
    public int id;

    [SerializeField] private MeshRenderer overlay;
    [SerializeField] private Material checkMaterial;
    [SerializeField] private Material validEndMaterial;
    [SerializeField] private Material invalidEndMaterial;
    
    private Track _track;

    private void OnEnable() {
        _track = GetComponentInParent<Track>();
        if (type == CheckpointType.Start) {
            overlay.enabled = false;
        }

        if (type == CheckpointType.Check) {
            overlay.material = checkMaterial;
        }

        if (type == CheckpointType.End) {
            overlay.material = invalidEndMaterial;
        }
    }

    public void Hit() {
        Debug.Log(type + " " + _track.IsEndCheckpointValid);
        if (type == CheckpointType.End && !_track.IsEndCheckpointValid) {
            return;
        }
        _track.CheckpointHit(this);
        overlay.enabled = false;
    }

    public void Update() {
        if (type == CheckpointType.End && _track.IsEndCheckpointValid) {
            overlay.material = validEndMaterial;
        }
    }
}
