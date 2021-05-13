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
    // Set to true by the level loader but may be overridden for testing
    [SerializeField] private bool ready;
    private bool _complete;
    private User _user;
    
    public bool IsEndCheckpointValid => hitCheckpoints.Count >= _checkpoints.Count - 2; // remove start and end

    private float timeMs;
    
    public void Awake() {
        _checkpoints = GetComponentsInChildren<Checkpoint>().ToList();
    }

    private void OnEnable() {
        Game.OnRestart += ResetTrack;
    }

    private void OnDisable() {
        Game.OnRestart -= ResetTrack;
    }

    public void ResetTrack() {
        var start = _checkpoints.Find(c => c.Type == CheckpointType.Start);
        if (start) {
            var ship = FindObjectOfType<Ship>();
            if (ship) {
                var startTransform = start.transform;
                ship.transform.position = new Vector3 {
                    x = startTransform.position.x,
                    y = startTransform.position.y,
                    z = startTransform.position.z
                };
            }
        }
        else {
            Debug.LogWarning("Checkpoints loaded with no start block! Is this intentional?");
        }
        
        _checkpoints.ForEach(c => {
            c.Reset();
        });
        
        hitCheckpoints = new List<Checkpoint>();
        ResetTimer();
    }

    public void ResetTimer() {
        _complete = false;
        timeMs = 0;
    }

    public void StartTimer() {
        ResetTimer();
        ready = true;
        _complete = false;
    }

    public void FinishTimer() {
        _complete = true;
    }

    public void CheckpointHit(Checkpoint checkpoint) {
        if (ready) {
            var hitCheckpoint = hitCheckpoints.Find(c => c == checkpoint);

            if (hitCheckpoint && hitCheckpoint.Type == CheckpointType.End) {
                _user.totalTimeDisplay.GetComponent<Text>().color = new Color(0, 1, 0, 1);

                if (!FindObjectOfType<Game>().ShipParameters.ToJsonString()
                    .Equals(Ship.ShipParameterDefaults.ToJsonString())) {
                    // you dirty debug cheater!
                    _user.totalTimeDisplay.GetComponent<Text>().color = new Color(1, 1, 0, 1);
                }

                FinishTimer();
            }

            if (!hitCheckpoint) {
                // new checkpoint, record it and split timer
                hitCheckpoints.Add(checkpoint);
                // update split display and fade out
                if (checkpoint.Type == CheckpointType.Check) {
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
        ResetTrack();
    }
    
    private void FixedUpdate() {
        // failing to get user in early stages due to modular loading? 
        if (!_user) {
            _user = FindObjectOfType<User>();
            return;
        }

        if (ready && !_complete && _user.totalTimeDisplay != null) {
            _user.totalTimeDisplay.textBox.color = new Color(1f, 1f, 1f, 1f);
            timeMs += Time.fixedDeltaTime;
            _user.totalTimeDisplay.SetTimeMs(timeMs);
        }
    }
}
