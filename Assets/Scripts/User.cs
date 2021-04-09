using System;
using System.Collections;
using System.Collections.Generic;
using Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class User : MonoBehaviour {

    [SerializeField] public PauseMenu pauseMenu;
    [SerializeField] public Ship playerShip;
    [SerializeField] public InputSystemUIInputModule pauseUIInputModule;

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
        GetComponent<PlayerInput>().enabled = true;
        pauseUIInputModule.enabled = false;
    }
    public void DisableGameInput() {
        GetComponent<PlayerInput>().enabled = false;
        pauseUIInputModule.enabled = true;
    }
    
    /**
     * Event responders for PlayerInput, only valid in-game.
     *  UI Requires additional bootstrap as above because UI events in Unity are fucking bonkers.
     */
    public void OnShowGameMenu() {
        pauseMenu.OnGameMenuToggle();
    }

    public void OnPitch(InputValue value) {
        playerShip.OnPitch(value);
    }

    public void OnRoll(InputValue value) {
        playerShip.OnRoll(value);
    }

    public void OnYaw(InputValue value) {
        playerShip.OnYaw(value);
    }

    public void OnThrottle(InputValue value) {
        playerShip.OnThrottle(value);
    }
    
    public void OnLateralH(InputValue value) {
        playerShip.OnLateralH(value);
    }
    
    public void OnLateralV(InputValue value) {
        playerShip.OnLateralV(value);
    }

    public void OnBoost(InputValue value) {
        playerShip.OnBoost(value);
    }

    public void OnFlightAssistToggle(InputValue value) {
        playerShip.OnFlightAssistToggle(value);
    }
}
