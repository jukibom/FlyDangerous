using System.Collections;
using System.Collections.Generic;
using Menus;
using UnityEngine;
using UnityEngine.InputSystem;

public class User : MonoBehaviour {

    [SerializeField] public PauseMenu pauseMenu;
    [SerializeField] public Ship playerShip;

    public void EnableGameInput() {
        GetComponent<PlayerInput>().enabled = true;
    }

    public void DisableGameInput() {
        GetComponent<PlayerInput>().enabled = false;
    }
    
    public void OnGameMenuToggle() {
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
