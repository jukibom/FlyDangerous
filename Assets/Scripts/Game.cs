using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Canvas))]
public class Game : MonoBehaviour
{

    private FlyDangerousActions _gameActions;
    private Canvas _menuCanvas;

    private void Awake() {
        this._menuCanvas = GetComponent<Canvas>();
        
        // TODO: can I attach this as a component instead of this bs?
        this._gameActions = new FlyDangerousActions();

        this._gameActions.Global.GameMenuToggle.performed += ToggleMenu;
        this._gameActions.Global.GameMenuToggle.canceled += ToggleMenu;
    }
    
    private void OnEnable() {
        _gameActions.Enable();
    }

    private void OnDisable() {
        _gameActions.Disable();
    }

    public void ToggleMenu(InputAction.CallbackContext context) {
        if (context.ReadValueAsButton()) {
            this._menuCanvas.enabled = !this._menuCanvas.enabled;
        }
    }
}
