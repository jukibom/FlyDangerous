using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Core;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class Track : MonoBehaviour {

    public List<Checkpoint> hitCheckpoints;

    public List<Checkpoint> Checkpoints {
        get => GetComponentsInChildren<Checkpoint>().ToList();
        set => ReplaceCheckpoints(value);
    }
    // Set to true by the level loader but may be overridden for testing
    [SerializeField] private bool isActive;
    private bool _complete;
    [CanBeNull] private User _user;
    
    public bool IsEndCheckpointValid => hitCheckpoints.Count >= Checkpoints.Count - 2; // remove start and end

    private float timeMs;

    private void OnEnable() {
        Game.OnRestart += InitialiseTrack;
    }

    private void OnDisable() {
        Game.OnRestart -= InitialiseTrack;
    }

    public void InitialiseTrack() {
        var start = Checkpoints.Find(c => c.Type == CheckpointType.Start);
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
        
        Checkpoints.ForEach(c => {
            c.Reset();
        });
        
        hitCheckpoints = new List<Checkpoint>();
        if (isActive) {
            ResetTimer();
            StopTimer();
        }
    }

    public void ResetTimer() {
        _complete = false;
        timeMs = 0;
        
        // reset timer text to 0, hide split timer
        if (_user) {
            _user.totalTimeDisplay.SetTimeMs(0);
            _user.totalTimeDisplay.textBox.color = new Color(1f, 1f, 1f, 1);
            _user.splitTimeDisplay.textBox.color = new Color(1f, 1f, 1f, 0);
        }
    }

    public void StopTimer() {
        isActive = false;
        _complete = false;
    }

    public IEnumerator StartTrackWithCountdown() {
        if (_user != null) {
            if (Checkpoints.Count > 0) {
                ResetTimer();
                timeMs = -2.5f;
                isActive = true;
                _complete = false;
                
                // enable user input but disable actual movement
                _user.EnableGameInput();
                _user.movementEnabled = false;
                _user.pauseMenuEnabled = false;
                
                // half a second (2.5 second total) before countdown
                yield return new WaitForSeconds(0.5f);
                
                // start countdown sounds
                AudioManager.Instance.Play("tt-countdown");
                
                // second beep (boost available here)
                yield return new WaitForSeconds(1);
                _user.boostButtonEnabledOverride = true;
                
                // GO!
                yield return new WaitForSeconds(1);
                _user.movementEnabled = true;
                _user.pauseMenuEnabled = true;
            }
        }
        yield return new WaitForEndOfFrame();
    }

    public void FinishTimer() {
        _complete = true;
    }

    public void CheckpointHit(Checkpoint checkpoint) {
        if (isActive) {
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
        foreach (var checkpoint in Checkpoints) {
            Destroy(checkpoint.gameObject);
        }

        hitCheckpoints = new List<Checkpoint>();
        InitialiseTrack();
    }
    
    private void FixedUpdate() {
        // failing to get user in early stages due to modular loading? 
        if (!_user) {
            _user = FindObjectOfType<User>();
            return;
        }

        if (isActive && !_complete && _user.totalTimeDisplay != null) {
            _user.totalTimeDisplay.textBox.color = new Color(1f, 1f, 1f, 1f);
            timeMs += Time.fixedDeltaTime;
            _user.totalTimeDisplay.SetTimeMs(Math.Abs(timeMs));
        }
    }
}
