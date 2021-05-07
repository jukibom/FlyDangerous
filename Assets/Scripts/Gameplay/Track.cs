using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Den.Tools;
using Engine;
using UnityEngine;
using UnityEngine.UI;

public class Track : MonoBehaviour {

    public List<Checkpoint> hitCheckpoints;
    [SerializeField] private List<Checkpoint> _checkpoints;

    public List<Checkpoint> Checkpoints {
        get => _checkpoints;
        set => ReplaceCheckpoints(value);
    }
    private Text timeText;
    private bool _ready;
    private bool _started;
    private bool _complete;
    private User _user;
    
    public bool IsEndCheckpointValid => hitCheckpoints.Count >= _checkpoints.Count - 1;

    private float timeMs = 0;
    
    public void Awake() {
        _checkpoints = GetComponentsInChildren<Checkpoint>().ToList();
    }

    public void TrackReady() {
        _ready = true;
        var start = _checkpoints.Find(c => c.type == CheckpointType.Start);
        if (start) {
            start.ShowOverlay();
        }
    }

    public void ResetTimer() {
        _started = false;
        _complete = false;
        timeMs = 0;
    }

    public void StartTimer() {
        ResetTimer();
        _started = true;
        _complete = false;
    }

    public void FinishTimer() {
        _started = false;
        _complete = true;
    }

    public void CheckpointHit(Checkpoint checkpoint) {
        if (_ready) {
            var hitCheckpoint = hitCheckpoints.Find(c => c == checkpoint);

            if (checkpoint.type == CheckpointType.Start) {
                if (!_started) {
                    hitCheckpoints = new List<Checkpoint>();
                    foreach (var c in _checkpoints) {
                        c.ShowOverlay();
                    }

                    StartTimer();
                }
            }

            if (hitCheckpoint && hitCheckpoint.type == CheckpointType.End) {
                _user.totalTimeDisplay.GetComponent<Text>().color = new Color(0, 1, 0, 1);

                if (!FindObjectOfType<Game>().ShipParameters.ToJsonString()
                    .Equals(Ship.ShipParameterDefaults.ToJsonString())) {
                    // you dirty debug cheater!
                    _user.totalTimeDisplay.GetComponent<Text>().color = new Color(1, 1, 0, 1);
                }

                FinishTimer();

                // reset start location
                if (Game.Instance.LevelDataCurrent.raceType != RaceType.Sprint) {
                    var c = _checkpoints.Find(c => c.type == CheckpointType.Start);
                    if (c) {
                        c.ShowOverlay();
                    }
                }
            }

            if (!hitCheckpoint) {
                // new checkpoint, record it and split timer
                hitCheckpoints.Add(checkpoint);
                Debug.Log("HIT");
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
    }

    private void ReplaceCheckpoints(List<Checkpoint> checkpoints) {
        foreach (var checkpoint in _checkpoints) {
            Destroy(checkpoint.gameObject);
        }

        hitCheckpoints = new List<Checkpoint>();
        _checkpoints = checkpoints;
    }
    
    private void FixedUpdate() {
        // failing to get user in early stages due to modular loading? 
        if (!_user) {
            _user = FindObjectOfType<User>();
            return;
        }

        if (_ready && _started && !_complete && _user.totalTimeDisplay != null) {
            _user.totalTimeDisplay.textBox.color = new Color(1f, 1f, 1f, 1f);
            timeMs += Time.fixedDeltaTime;
            _user.totalTimeDisplay.SetTimeMs(timeMs);
        }
    }
}
