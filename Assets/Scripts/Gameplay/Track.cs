using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Track : MonoBehaviour {

    public List<Checkpoint> hitCheckpoints;
    private Checkpoint[] _totalCheckpoints;
    private Text timeText;
    private bool _started;
    private bool _complete;
    private User _user;
    
    public bool IsEndCheckpointValid => hitCheckpoints.Count >= _totalCheckpoints.Length - 1;

    private float timeMs = 0;
    
    public void Awake() {
        _totalCheckpoints = GetComponentsInChildren<Checkpoint>();
    }

    public void ResetTimer() {
        _started = false;
        _complete = false;
        timeMs = 0;
    }

    public void StartTimer() {
        ResetTimer();
        _started = true;
    }

    public void CheckpointHit(Checkpoint checkpoint) {
        var hitCheckpoint = hitCheckpoints.Find(c => c.id == checkpoint.id);

        if (hitCheckpoint && hitCheckpoint.type == CheckpointType.End) {
            _user.totalTimeDisplay.GetComponent<Text>().color = new Color(0, 1, 0, 1);
            _complete = true;
        }
        
        if (!hitCheckpoint) {
            // new checkpoint, record it and split timer
            hitCheckpoints.Add(checkpoint);

            // update split display and fade out
            if (checkpoint.type == CheckpointType.Check) {
                _user.splitTimeDisplay.SetTimeMs(timeMs);
                
                // TODO: make this fade-out generic and reuse across the copy notification in pause menu
                _user.splitTimeDisplay.textBox.color = new Color(1f, 1f, 1f, 1f);

                IEnumerator FadeText() {
                    while (_user.splitTimeDisplay.textBox.color.a > 0.0f) {
                        _user.splitTimeDisplay.textBox.color = new Color(1f, 1f, 1f,
                        _user.splitTimeDisplay.textBox.color.a - (Time.unscaledDeltaTime / 2));
                        yield return null;
                    }
                }

                StartCoroutine(FadeText());
            }
        }
    }
    
    private void FixedUpdate() {
        // failing to get user in early stages due to modular loading? 
        if (!_user) {
            _user = FindObjectOfType<User>();
            return;
        }

        if (_started && !_complete && _user.totalTimeDisplay != null) {
            _user.totalTimeDisplay.textBox.color = new Color(1f, 1f, 1f, 1f);
            timeMs += Time.fixedDeltaTime;
            _user.totalTimeDisplay.SetTimeMs(timeMs);
        }
    }
}
