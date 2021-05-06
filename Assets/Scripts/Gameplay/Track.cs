using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Track : MonoBehaviour {

    public List<Checkpoint> hitCheckpoints;
    private Checkpoint[] _totalCheckpoints;
    private Text timeText;
    private TimeDisplay _timeDisplay;
    private bool _complete;
    
    public bool IsEndCheckpointValid => hitCheckpoints.Count >= _totalCheckpoints.Length - 1;

    private float timeMs = 0;
    
    public void Start() {
        _totalCheckpoints = GetComponentsInChildren<Checkpoint>();
        _timeDisplay = FindObjectOfType<TimeDisplay>();
    }

    public void CheckpointHit(Checkpoint checkpoint) {
        var hitCheckpoint = hitCheckpoints.Find(c => c.id == checkpoint.id);

        if (hitCheckpoint && hitCheckpoint.type == CheckpointType.End) {
            _timeDisplay.GetComponent<Text>().color = new Color(0, 1, 0, 1);
            _complete = true;
        }
        
        if (!hitCheckpoint) {
            // new checkpoint, record it and split timer
            hitCheckpoints.Add(checkpoint);
        }
    }

    private void FixedUpdate() {
        if (!_complete && _timeDisplay != null) {
            timeMs += Time.fixedDeltaTime;
            _timeDisplay.SetTimeMs(timeMs);
        }
    }
}
