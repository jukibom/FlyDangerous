using System;
using System.Collections;
using System.Collections.Generic;
using Engine;
using Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public class User : MonoBehaviour {

    [SerializeField] public PauseMenu pauseMenu;
    [SerializeField] public Ship playerShip;
    [SerializeField] public InputSystemUIInputModule pauseUIInputModule;
    [SerializeField] public MouseWidget mouseWidget;
    private bool _alternateFlightControls = false;

    private Vector2 _mousePositionScreen;
    private Vector2 _mousePositionNormalisedToSquare;

    private Action<InputAction.CallbackContext> _cancelAction;

    /** Boostrap global ESC / cancel action in UI */
    public void Awake() {
        _cancelAction = (context) => { OnShowGameMenu(); };
    }

    public void OnEnable() {
        pauseUIInputModule.cancel.action.performed += _cancelAction;
    }

    public void OnDisable() {
        pauseUIInputModule.cancel.action.performed -= _cancelAction;
    }

    public void Update() {
        // handle mouse input
        if (!pauseMenu.IsPaused && Preferences.Instance.GetBool("enableMouseFlightControls")) {
            // TODO: mouse sensitivity / scaling here (multiply normalised values by scaling factor);
            
            Action<string, float> setInput = (axis, amount) => {
                switch (axis) {
                    // TODO: mouse axis invert (fml)
                    case "pitch": playerShip.OnPitch(amount * -1);
                        break;
                    case "roll": playerShip.OnRoll(amount);
                        break;
                    case "yaw": playerShip.OnYaw(amount);
                        break;
                }
            };
            
            var xAxis = Preferences.Instance.GetString("mouseXAxis");
            var yAxis = Preferences.Instance.GetString("mouseYAxis");

            setInput(xAxis, _mousePositionNormalisedToSquare.x);
            setInput(yAxis, _mousePositionNormalisedToSquare.y);

            // relative mouse means reset after input
            if (Preferences.Instance.GetBool("relativeMouseXAxis")) {
                _mousePositionScreen.x = Screen.width / 2f;
                _mousePositionNormalisedToSquare.x = 0;
                Mouse.current.WarpCursorPosition(_mousePositionScreen);
            }
            else {
                mouseWidget.mousePositionNormalised.x = _mousePositionNormalisedToSquare.x;
            }

            if (Preferences.Instance.GetBool("relativeMouseYAxis")) {
                _mousePositionScreen.y = Screen.height / 2f;
                _mousePositionNormalisedToSquare.y = 0;
                Mouse.current.WarpCursorPosition(_mousePositionScreen);
            }
            else {
                mouseWidget.mousePositionNormalised.y = _mousePositionNormalisedToSquare.y;
            }
        }
    }

    /**
     * Enable and Disable input modules depending on context (in-game vs UI).
     * This prevents conflicts between the two, especially when rebinding keys...
     */
    public void EnableGameInput() {
        GetComponent<PlayerInput>().ActivateInput();
    }

    public void DisableGameInput() {
        GetComponent<PlayerInput>().DeactivateInput();
    }

    public void EnableUIInput() {
        pauseUIInputModule.enabled = true;
    }

    public void DisableUIInput() {
        pauseUIInputModule.enabled = false;
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
        if (!_alternateFlightControls)
            playerShip.OnPitch(value.Get<float>());
    }

    public void OnPitchAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnPitch(value.Get<float>());
    }

    public void OnRoll(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnRoll(value.Get<float>());
    }

    public void OnRollAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnRoll(value.Get<float>());
    }

    public void OnYaw(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnYaw(value.Get<float>());
    }

    public void OnYawAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnYaw(value.Get<float>());
    }

    public void OnThrottle(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnThrottle(value.Get<float>());
    }

    public void OnThrottleAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnThrottle(value.Get<float>());
    }

    public void OnLateralH(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnLateralH(value.Get<float>());
    }

    public void OnLateralHAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnLateralH(value.Get<float>());
    }

    public void OnLateralV(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnLateralV(value.Get<float>());
    }

    public void OnLateralVAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnLateralV(value.Get<float>());
    }

    public void OnBoost(InputValue value) {
        playerShip.OnBoost(value.isPressed);
    }

    public void OnFlightAssistToggle(InputValue value) {
        playerShip.OnFlightAssistToggle();
    }

    public void OnVelocityLimiter(InputValue value) {
        playerShip.OnVelocityLimiter(value.isPressed);
    }

    public void OnAltFlightControlsToggle(InputValue value) {
        _alternateFlightControls = !_alternateFlightControls;
    }

    public void OnRawMouse(InputValue value) {
        _mousePositionScreen = value.Get<Vector2>();

        if (_mousePositionScreen != Vector2.zero) {
            _mousePositionNormalisedToSquare = new Vector2(
                ((_mousePositionScreen.x / Screen.width * 2) - 1),
                (_mousePositionScreen.y / Screen.height * 2 - 1)
            );

            mouseWidget.UpdateWidgetSprites(_mousePositionNormalisedToSquare);
        }
    }
}
