using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour {

    public List<Checkpoint> hitCheckpoints;
    private Checkpoint[] _totalCheckpoints;

    public bool IsEndCheckpointValid => hitCheckpoints.Count >= _totalCheckpoints.Length - 1;

    public void Start() {
        _totalCheckpoints = GetComponentsInChildren<Checkpoint>();
    }

    public void CheckpointHit(Checkpoint checkpoint) {
        var hitCheckpoint = hitCheckpoints.Find(c => c.id == checkpoint.id);

        if (hitCheckpoint && hitCheckpoint.type == CheckpointType.End) {
            // magic end checkpoint code, we don't want to store this yet
            return;
        }
        
        if (!hitCheckpoint) {
            // new checkpoint, record it and split timer
            hitCheckpoints.Add(checkpoint);
        }
    }
}
