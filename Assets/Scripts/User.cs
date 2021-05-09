using System;
using Audio;
using Engine;
using Menus;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

public class User : MonoBehaviour {

    [SerializeField] public PauseMenu pauseMenu;
    [SerializeField] public Ship playerShip;
    [SerializeField] public InputSystemUIInputModule pauseUIInputModule;
    [SerializeField] public MouseWidget mouseWidget;
    [SerializeField] public TimeDisplay totalTimeDisplay;
    [SerializeField] public TimeDisplay splitTimeDisplay;
    private bool _alternateFlightControls = false;

    private Vector2 _mousePositionScreen;
    private Vector2 _mousePositionNormalized;
    private Vector2 _mousePositionNormalizedDelta;

    private float _pitch;
    private float _roll;
    private float _yaw;

    private bool _inputEnabled = true;

    private Action<InputAction.CallbackContext> _cancelAction;

    /** Boostrap global ESC / cancel action in UI */
    public void Awake() {
        _cancelAction = (context) => { OnShowGameMenu(); };
        ResetMouseToCentre();
    }

    public void OnEnable() {
        pauseUIInputModule.cancel.action.performed += _cancelAction;
        ResetMouseToCentre();
    }

    public void OnDisable() {
        pauseUIInputModule.cancel.action.performed -= _cancelAction;
    }

    public void Update() {

        if (!_inputEnabled) {
            return;
        }

        var pitch = _pitch;
        var roll = _roll;
        var yaw = _yaw;

        if (!pauseMenu.IsPaused && Preferences.Instance.GetBool("enableMouseFlightControls")) {
            
            var relativeX = Preferences.Instance.GetBool("relativeMouseXAxis");
            var relativeY = Preferences.Instance.GetBool("relativeMouseYAxis");
            var mouseXAxisBind = Preferences.Instance.GetString("mouseXAxis");
            var mouseYAxisBind = Preferences.Instance.GetString("mouseYAxis");
            
            float sensitivityX = Preferences.Instance.GetFloat("mouseXSensitivity");
            float sensitivityY = Preferences.Instance.GetFloat("mouseYSensitivity");

            Action<string, float> setInput = (axis, amount) => {
                switch (axis) {
                    // TODO: mouse axis invert (fml)
                    case "pitch": pitch += amount * -1;
                        break;
                    case "roll": roll += amount;
                        break;
                    case "yaw": yaw += amount;
                        break;
                }
            };

            if (relativeX) {
                setInput(mouseXAxisBind, _mousePositionNormalizedDelta.x * sensitivityX);
            }
            else {
                setInput(mouseXAxisBind, _mousePositionNormalized.x * sensitivityX);
            }

            if (relativeY) {
                setInput(mouseYAxisBind, _mousePositionNormalizedDelta.y * sensitivityY);
            }
            else {
                setInput(mouseYAxisBind, _mousePositionNormalized.y * sensitivityY);
            }

            Vector2 widgetPosition = new Vector2(
                relativeX ? (_mousePositionNormalizedDelta.x * 0.01f) : _mousePositionNormalized.x,
                relativeY ? (_mousePositionNormalizedDelta.y * 0.01f) : _mousePositionNormalized.y
            );
            mouseWidget.UpdateWidgetSprites(widgetPosition);
        }
        
        playerShip.SetPitch(pitch);
        playerShip.SetRoll(roll);
        playerShip.SetYaw(yaw);
    }

    /**
     * Enable and Disable input modules depending on context (in-game vs UI).
     * This prevents conflicts between the two, especially when rebinding keys...
     */
    public void EnableGameInput() {
        GetComponent<PlayerInput>().ActivateInput();
        _inputEnabled = true;
    }

    public void DisableGameInput() {
        GetComponent<PlayerInput>().DeactivateInput();
        _inputEnabled = false;
    }

    public void EnableUIInput() {
        pauseUIInputModule.enabled = true;
    }

    public void DisableUIInput() {
        pauseUIInputModule.enabled = false;
    }

    public void ResetMouseToCentre() {
        var warpedPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Mouse.current.WarpCursorPosition(warpedPosition);
        InputState.Change(Mouse.current.position, warpedPosition);
        _mousePositionScreen = warpedPosition;
        _mousePositionNormalized = new Vector2(0, 0);
        _mousePositionNormalizedDelta = new Vector2(0, 0); 
    }

    /**
     * Event responders for PlayerInput, only valid in-game.
     *  UI Requires additional bootstrap as above because UI events in Unity are fucking bonkers.
     */
    public void OnShowGameMenu() {
        pauseMenu.OnGameMenuToggle();
    }

    public void OnRestartTrack() {
        Game.Instance.RestartLevel();
    }

    public void OnRestartFromLastCheckpoint() {
        Debug.Log("Lol there are no checkpoints yet ^_^");
    }

    public void OnPitch(InputValue value) {
        if (!_alternateFlightControls) _pitch = value.Get<float>();
    }

    public void OnPitchAlt(InputValue value) {
        if (_alternateFlightControls) _pitch = value.Get<float>();
    }

    public void OnRoll(InputValue value) {
        if (!_alternateFlightControls) _roll = value.Get<float>();
    }

    public void OnRollAlt(InputValue value) {
        if (_alternateFlightControls) _roll = value.Get<float>();
    }

    public void OnYaw(InputValue value) {
        if (!_alternateFlightControls) _yaw = value.Get<float>();
    }

    public void OnYawAlt(InputValue value) {
        if (_alternateFlightControls) _yaw = value.Get<float>();
    }

    public void OnThrottle(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.SetThrottle(value.Get<float>());
    }

    public void OnThrottleAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.SetThrottle(value.Get<float>());
    }

    public void OnLateralH(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.SetLateralH(value.Get<float>());
    }

    public void OnLateralHAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.SetLateralH(value.Get<float>());
    }

    public void OnLateralV(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.SetLateralV(value.Get<float>());
    }

    public void OnLateralVAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.SetLateralV(value.Get<float>());
    }

    public void OnBoost(InputValue value) {
        playerShip.Boost(value.isPressed);
    }

    public void OnFlightAssistToggle(InputValue value) {
        playerShip.FlightAssistToggle();
    }

    public void OnVelocityLimiter(InputValue value) {
        playerShip.VelocityLimiterIsPressed(value.isPressed);
    }

    public void OnAltFlightControlsToggle(InputValue value) {
        _pitch = 0;
        _roll = 0;
        _yaw = 0;
        _alternateFlightControls = !_alternateFlightControls;
        if (_alternateFlightControls) {
            AudioManager.Instance.Play("ship-alternate-flight-on");
        }
        else {
            AudioManager.Instance.Play("ship-alternate-flight-off");
        }
    }

    public void OnMouseRaw(InputValue value) {
        _mousePositionScreen = value.Get<Vector2>();
        _mousePositionNormalized = new Vector2(
            ((_mousePositionScreen.x / Screen.width * 2) - 1),
            (_mousePositionScreen.y / Screen.height * 2 - 1)
        );
    }

    public void OnMouseRawNormalizedDelta(InputValue value) {
        _mousePositionNormalizedDelta = value.Get<Vector2>();
    }

    public void OnToggleConsole(InputValue value) {
        if (Console.Instance.Visible) {
            Console.Instance.Hide();
        }
        else {
            Console.Instance.Show();
        }
    }
}
