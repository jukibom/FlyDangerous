using Core;
using UnityEngine;

public class SpaceStation : MonoBehaviour {
    public float rotationAmountDegrees = 0.1f;
    public Rigidbody centralSection;

    private Quaternion _initialRotation;

    private bool _hasStarted;

    private void FixedUpdate() {
        if (!_hasStarted) return;
        
        var deltaRotation = Quaternion.Euler(new Vector3(0, 0, rotationAmountDegrees));
        centralSection.MoveRotation(centralSection.rotation * deltaRotation);
    }

    private void OnEnable() {
        Game.OnGameModeStart += OnGameModeStart;
        Game.OnRestart += OnRestart;
    }

    private void OnDisable() {
        Game.OnGameModeStart -= OnGameModeStart;
        Game.OnRestart -= OnRestart;
    }

    private void OnGameModeStart() {
        if (!_hasStarted) {
            _hasStarted = true;
            _initialRotation = centralSection.rotation;
        }
        ResetStation();
    }

    private void OnRestart() {
        _hasStarted = false;
        ResetStation();
    }

    private void ResetStation() {
        centralSection.MoveRotation(_initialRotation);
        centralSection.transform.rotation = _initialRotation;
    }
}