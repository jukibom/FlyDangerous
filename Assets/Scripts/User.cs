using System;
using System.Collections;
using System.Collections.Generic;
using Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public class User : MonoBehaviour {

    [SerializeField] public PauseMenu pauseMenu;
    [SerializeField] public Ship playerShip;
    [SerializeField] public InputSystemUIInputModule pauseUIInputModule;
    private bool _alternateFlightControls = false;

    private Action<InputAction.CallbackContext> _cancelAction;

    /** Boostrap global ESC / cancel action in UI */
    public void Awake() {
        _cancelAction = (context) => {
            OnShowGameMenu();
        };
    }
    public void OnEnable() {
        pauseUIInputModule.cancel.action.performed += _cancelAction;
    }
    public void OnDisable() {
        pauseUIInputModule.cancel.action.performed -= _cancelAction;
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

    public void OnRestartFromLastCheckpoint() {
        Debug.Log("Lol there are no checkpoints yet ^_^");
    }

    public void OnPitch(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnPitch(value);
    }

    public void OnPitchAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnPitch(value);
    }

    public void OnRoll(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnRoll(value);
    }

    public void OnRollAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnRoll(value);
    }

    public void OnYaw(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnYaw(value);
    }

    public void OnYawAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnYaw(value);
    }

    public void OnThrottle(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnThrottle(value);
    }

    public void OnThrottleAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnThrottle(value);
    }
    
    public void OnLateralH(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnLateralH(value);
    }
    
    public void OnLateralHAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnLateralH(value);
    }
    
    public void OnLateralV(InputValue value) {
        if (!_alternateFlightControls)
            playerShip.OnLateralV(value);
    }
    
    public void OnLateralVAlt(InputValue value) {
        if (_alternateFlightControls)
            playerShip.OnLateralV(value);
    }

    public void OnBoost(InputValue value) {
        playerShip.OnBoost(value);
    }

    public void OnFlightAssistToggle(InputValue value) {
        playerShip.OnFlightAssistToggle(value);
    }

    public void OnVelocityLimiter(InputValue value) {
        playerShip.OnVelocityLimiter(value);
    }

    public void OnAltFlightControlsToggle(InputValue value) {
        _alternateFlightControls = !_alternateFlightControls;
    }

    public void OnRawMouse(InputValue value) {
        
    }
}
