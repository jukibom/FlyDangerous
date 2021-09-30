using System;
using System.Linq;
using Audio;
using Game_UI;
using Menus.Pause_Menu;
using Misc;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace Core.Player {
    public class User : MonoBehaviour {

        [SerializeField] public ShipPlayer shipPlayer;

        [SerializeField] public PauseMenu pauseMenu;
        [SerializeField] public Canvas uiCanvas;
        [SerializeField] public GameObject flatScreenCamera;
        [SerializeField] public Camera flatScreenGameplayCamera;
        [SerializeField] public XRRig xrRig;

        [SerializeField] public InputSystemUIInputModule pauseUIInputModule;
        [SerializeField] public MouseWidget mouseWidget;
        [SerializeField] public TimeDisplay totalTimeDisplay;
        [SerializeField] public TimeDisplay splitTimeDisplay;
        [SerializeField] public TimeDisplay splitTimeDeltaDisplay;
        [SerializeField] public TimeDisplay targetTimeDisplay;
        [SerializeField] public Text targetTimeTypeDisplay;
        private bool _alternateFlightControls;

        private Vector2 _mousePositionScreen;
        private Vector2 _mousePositionNormalized;
        private Vector2 _mousePositionDelta;
        private Vector2 _previousRelativeRate;

        private float _pitch;
        private float _roll;
        private float _yaw;
        private float _throttle;
        private float _lateralH;
        private float _lateralV;
        private bool _boost;
        private bool _reverse;
        private float _targetThrottle;
        private float _targetThrottleIncrement;

        [SerializeField] public bool movementEnabled;
        public bool pauseMenuEnabled = true;
        public bool boostButtonEnabledOverride;
        
        public Transform UserHeadTransform => 
            Game.Instance.IsVREnabled
                ? xrRig.gameObject.GetComponentInChildren<Camera>().transform
                : flatScreenCamera.gameObject.GetComponentInChildren<Camera>().transform;

        private Action<InputAction.CallbackContext> _cancelAction;

        /** Boostrap global ESC / cancel action in UI */
        public void Awake() {
            _cancelAction = context => { pauseMenu.OnGameMenuToggle(); };
            DisableGameInput();
            ResetMouseToCentre();
        }

        public void Start() {
            // if there's no controlling gamestate loaded, enable own input (usually in editor) 
            if (!FindObjectOfType<Game>()) {
                EnableGameInput();
            }
        }

        public void OnEnable() {
            pauseUIInputModule.cancel.action.performed += _cancelAction;
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
            Game.OnVRStatus += SetVRStatus;
            ResetMouseToCentre();
            FdConsole.Instance.Clear();
        }

        public void OnDisable() {
            pauseUIInputModule.cancel.action.performed -= _cancelAction;
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
            Game.OnVRStatus -= SetVRStatus;
        }

        public void OnGameSettingsApplied() {
            foreach (var gameCamera in flatScreenGameplayCamera.GetComponentsInChildren<Camera>()) {
                gameCamera.fieldOfView = Preferences.Instance.GetFloat("graphics-field-of-view");
            }
        }

        public void Update() {
            if (movementEnabled) {
                var pitch = _pitch;
                var roll = _roll;
                var yaw = _yaw;
                var throttle = _throttle;
                var lateralH = _lateralH;
                var lateralV = _lateralV;

                if (Preferences.Instance.GetString("throttleType") == "forward only") {
                    throttle = MathfExtensions.Remap(-1, 1, 0, 1, throttle);
                    if (_reverse) {
                        throttle *= -1;
                    }
                }

                _targetThrottle = MathfExtensions.Clamp(-1, 1, _targetThrottle + _targetThrottleIncrement);
                if (_targetThrottle != 0) {
                    throttle = _targetThrottle;
                }
                
                if (!pauseMenu.IsPaused && Preferences.Instance.GetBool("enableMouseFlightControls")) {
                    CalculateMouseInput(out var mousePitch, out var mouseRoll, out var mouseYaw);
                    pitch += mousePitch;
                    roll += mouseRoll;
                    yaw += mouseYaw;
                }

                shipPlayer.SetPitch(pitch);
                shipPlayer.SetRoll(roll);
                shipPlayer.SetYaw(yaw);
                shipPlayer.SetThrottle(throttle);
                shipPlayer.SetLateralH(lateralH);
                shipPlayer.SetLateralV(lateralV);
                shipPlayer.Boost(_boost);
            }

            if (boostButtonEnabledOverride) {
                shipPlayer.Boost(_boost);
            }
        }

        /**
     * Enable and Disable input modules depending on context (in-game vs UI).
     * This prevents conflicts between the two, especially when rebinding keys...
     */
        public void EnableGameInput() {
            var playerInput = GetComponent<PlayerInput>();
            playerInput.ActivateInput();
            
            // enable multiple input action sets
            playerInput.actions.FindActionMap("Global").Enable();
            playerInput.actions.FindActionMap("Ship").Enable();
            
            movementEnabled = true;
            pauseMenuEnabled = true;
            boostButtonEnabledOverride = false;

            FdConsole.Instance.LogMessage("** USER INPUT ENABLED **");
            foreach (var inputDevice in InputSystem.devices) {
                FdConsole.Instance.LogMessage(inputDevice.name + " with path <" + inputDevice.device.path + ">" +
                                            " detected");
                if (!playerInput.devices.Contains(inputDevice)) {
                    FdConsole.Instance.LogMessage(inputDevice.name + " not paired to user! Pairing ...");
                    InputUser.PerformPairingWithDevice(inputDevice, playerInput.user);
                }
            }

            FdConsole.Instance.LogMessage("---");
            foreach (var playerInputDevice in playerInput.devices) {
                FdConsole.Instance.LogMessage(playerInputDevice.name + " paired");
            }

            ResetMouseToCentre();
        }

        public void DisableGameInput() {
            var playerInput = GetComponent<PlayerInput>();

            // enable multiple input action sets
            playerInput.actions.FindActionMap("Ship").Disable();
            
            movementEnabled = false;
            pauseMenuEnabled = false;
            boostButtonEnabledOverride = false;
            
            // clear inputs
            shipPlayer.SetPitch(0);
            shipPlayer.SetRoll(0);
            shipPlayer.SetYaw(0);
            shipPlayer.SetThrottle(0);
            shipPlayer.SetLateralH(0);
            shipPlayer.SetLateralV(0);

            FdConsole.Instance.LogMessage("** USER INPUT DISABLED **");
        }

        public void EnableUIInput() {
            pauseUIInputModule.enabled = true;
        }

        public void DisableUIInput() {
            pauseUIInputModule.enabled = false;
        }

        public void SetVRStatus(bool isVREnabled) {
            // if VR is enabled, we need to swap our active cameras and make UI panels operate in world space
            if (isVREnabled) {
                var pauseMenuCanvas = pauseMenu.GetComponent<Canvas>();
                pauseMenuCanvas.renderMode = RenderMode.WorldSpace;
                uiCanvas.renderMode = RenderMode.WorldSpace;
                var pauseMenuRect = pauseMenuCanvas.GetComponent<RectTransform>();
                pauseMenuRect.sizeDelta = new Vector2(1280, 1000);
                pauseMenuRect.localScale /= 2;
                flatScreenCamera.SetActive(false);
                xrRig.gameObject.SetActive(true);
            }
            else {
                var pauseMenuCanvas = pauseMenu.GetComponent<Canvas>();
                pauseMenuCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                var pauseMenuRect = pauseMenuCanvas.GetComponent<RectTransform>();
                pauseMenuRect.sizeDelta = new Vector2(1920, 1080);
                pauseMenuRect.localScale *= 2;
                flatScreenCamera.SetActive(true);
                xrRig.gameObject.SetActive(false);
            }
        }

        public void ResetMouseToCentre() {
            var warpedPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Mouse.current.WarpCursorPosition(warpedPosition);
            InputState.Change(Mouse.current.position, warpedPosition);
            _mousePositionScreen = warpedPosition;
            _mousePositionNormalized = new Vector2(0, 0);
            _mousePositionDelta = new Vector2(0, 0);
            mouseWidget.ResetToCentre();
        }

        /**
     * Event responders for PlayerInput, only valid in-game.
     *  UI Requires additional bootstrap as above because UI events in Unity are fucking bonkers.
     */
        public void OnShowGameMenu() {
            if (pauseMenuEnabled) {
                pauseMenu.OnGameMenuToggle();
            }
        }

        public void OnRestartTrack() {
            if (movementEnabled) {
                Game.Instance.RestartSession();
            }
        }

        public void OnRestartFromLastCheckpoint() {
            if (movementEnabled) {
                Debug.Log("Lol there are no checkpoints yet ^_^");
            }
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
            _targetThrottle = 0;
            _targetThrottleIncrement = 0;
            if (!_alternateFlightControls) _throttle = value.Get<float>();
        }

        public void OnThrottleAlt(InputValue value) {
            _targetThrottle = 0;
            _targetThrottleIncrement = 0;
            if (_alternateFlightControls) _throttle = value.Get<float>();
        }

        public void OnThrottleIncrease(InputValue value) {
            if (value.isPressed) {
                _targetThrottleIncrement = MathfExtensions.Remap(0, 1, 0, 0.005f, value.Get<float>());
            }
            else {
                _targetThrottleIncrement = 0;
            }
        }

        public void OnThrottleDecrease(InputValue value) {
            if (value.isPressed) { 
                _targetThrottleIncrement = MathfExtensions.Remap(0, 1, 0, -0.005f, value.Get<float>());
            }
            else {
                _targetThrottleIncrement = 0;
            }
        }

        public void OnLateralH(InputValue value) {
            if (!_alternateFlightControls) _lateralH = value.Get<float>();
        }

        public void OnLateralHAlt(InputValue value) {
            if (_alternateFlightControls) _lateralH = value.Get<float>();
        }

        public void OnLateralV(InputValue value) {
            if (!_alternateFlightControls) _lateralV = value.Get<float>();
        }

        public void OnLateralVAlt(InputValue value) {
            if (_alternateFlightControls) _lateralV = value.Get<float>();
        }

        public void OnToggleReverse(InputValue value) {
            UIAudioManager.Instance.Play("ui-nav");
            _reverse = !_reverse;
        }

        public void OnBoost(InputValue value) {
            _boost = value.isPressed;
        }

        public void OnVelocityLimiter(InputValue value) {
            shipPlayer.VelocityLimiterIsPressed(value.isPressed);
        }

        public void OnAllFlightAssistToggle(InputValue value) {
            shipPlayer.AllFlightAssistToggle();
        }

        public void OnVectorFlightAssistToggle(InputValue value) {
            shipPlayer.FlightAssistVectorControlToggle();
        }

        public void OnRotationalFlightAssistToggle(InputValue value) {
            shipPlayer.FlightAssistRotationalDampeningToggle();
        }

        public void OnShipLightsToggle(InputValue value) {
            shipPlayer.ShipLightsToggle();
        }

        public void OnAltFlightControlsToggle(InputValue value) {
            _pitch = 0;
            _roll = 0;
            _yaw = 0;
            _throttle = 0;
            _lateralH = 0;
            _lateralV = 0;
            _alternateFlightControls = !_alternateFlightControls;
            if (_alternateFlightControls) {
                UIAudioManager.Instance.Play("ship-alternate-flight-on");
            }
            else {
                UIAudioManager.Instance.Play("ship-alternate-flight-off");
            }
        }

        public void OnMouseRawDelta(InputValue value) {
            _mousePositionDelta = value.Get<Vector2>();
            _mousePositionScreen.x += _mousePositionDelta.x;
            _mousePositionScreen.y += _mousePositionDelta.y;
            _mousePositionNormalized = new Vector2(
                ((_mousePositionScreen.x / Screen.width * 2) - 1),
                (_mousePositionScreen.y / Screen.height * 2 - 1)
            );
        }

        public void OnRecenterMouse(InputValue value) {
            ResetMouseToCentre();
        }

        public void OnResetHMDView(InputValue value) {
            if (xrRig) {
                Game.Instance.ResetHmdView(xrRig, transform);
            }
        }

        public void OnToggleConsole(InputValue value) {
            if (FdConsole.Instance.Visible) {
                FdConsole.Instance.Hide();
            }
            else {
                FdConsole.Instance.Show();
            }
        }

        private void CalculateMouseInput(out float pitchMouseInput, out float rollMouseInput, out float yawMouseInput) {

            float pitch = 0, roll = 0, yaw = 0;

            var mouseXAxisBind = Preferences.Instance.GetString("mouseXAxis");
            var mouseYAxisBind = Preferences.Instance.GetString("mouseYAxis");

            float sensitivityX = Preferences.Instance.GetFloat("mouseXSensitivity");
            float sensitivityY = Preferences.Instance.GetFloat("mouseYSensitivity");

            bool mouseXInvert = Preferences.Instance.GetBool("mouseXInvert");
            bool mouseYInvert = Preferences.Instance.GetBool("mouseXInvert");

            var mouseXIsRelative = Preferences.Instance.GetBool("relativeMouseXAxis") || (Preferences.Instance.GetBool("forceRelativeMouseWithFAOff") && !shipPlayer.IsRotationalFlightAssistActive);
            var mouseYIsRelative = Preferences.Instance.GetBool("relativeMouseYAxis") || (Preferences.Instance.GetBool("forceRelativeMouseWithFAOff") && !shipPlayer.IsRotationalFlightAssistActive);;

            float mouseRelativeRate = MathfExtensions.Clamp(1, 50f, Preferences.Instance.GetFloat("mouseRelativeRate"));

            float mouseDeadzone = MathfExtensions.Clamp(0, 1, Preferences.Instance.GetFloat("mouseDeadzone"));
            float mousePowerCurve = MathfExtensions.Clamp(1, 3, Preferences.Instance.GetFloat("mousePowerCurve"));

            // // get deadzone as a pixel value including sensitivity change
            var mouseDeadzoneX = mouseDeadzone * Mathf.Pow(sensitivityX, -1);
            var mouseDeadzoneY = mouseDeadzone * Mathf.Pow(sensitivityY, -1);

            // calculate continuous input including deadzone and sensitivity
            var continuousMouseX = 0f;
            var continuousMouseY = 0f;
            if (_mousePositionNormalized.x > mouseDeadzoneX) {
                continuousMouseX = (_mousePositionNormalized.x - mouseDeadzone) * sensitivityX;
            }

            if (_mousePositionNormalized.x < -mouseDeadzoneX) {
                continuousMouseX = (_mousePositionNormalized.x + mouseDeadzone) * sensitivityX;
            }

            if (_mousePositionNormalized.y > mouseDeadzoneY) {
                continuousMouseY = (_mousePositionNormalized.y - mouseDeadzone) * sensitivityY;
            }

            if (_mousePositionNormalized.y < -mouseDeadzoneY) {
                continuousMouseY = (_mousePositionNormalized.y + mouseDeadzone) * sensitivityY;
            }

            // calculate relative input from deltas including sensitivity
            var relativeMouse = new Vector2(_mousePositionDelta.x * sensitivityX, _mousePositionDelta.y * sensitivityY);
            relativeMouse += Vector2.MoveTowards(new Vector2(
                    _previousRelativeRate.x,
                    _previousRelativeRate.y),
                Vector2.zero, mouseRelativeRate);

            // power curve (Mathf.Pow does not allow negatives because REASONS so abs and multiply by -1 if the original val is < 0)
            continuousMouseX = (continuousMouseX < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(continuousMouseX), mousePowerCurve);
            continuousMouseY = (continuousMouseY < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(continuousMouseY), mousePowerCurve);
            relativeMouse.x = (relativeMouse.x < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(relativeMouse.x), mousePowerCurve);
            relativeMouse.y = (relativeMouse.y < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(relativeMouse.y), mousePowerCurve);

            // set the input for a given axis 
            Action<string, float, bool> setInput = (axis, amount, shouldInvert) => {
                var invert = shouldInvert ? -1 : 1;

                switch (axis) {
                    case "pitch":
                        pitch += amount * -1 * invert;
                        break;
                    case "roll":
                        roll += amount * invert;
                        break;
                    case "yaw":
                        yaw += amount * invert;
                        break;
                }
            };

            // send input depending on mouse mode
            if (mouseXIsRelative) {
                setInput(mouseXAxisBind, relativeMouse.x, mouseXInvert);
            }
            else {
                setInput(mouseXAxisBind, continuousMouseX, mouseXInvert);
            }

            if (mouseYIsRelative) {
                setInput(mouseYAxisBind, relativeMouse.y, mouseYInvert);
            }
            else {
                setInput(mouseYAxisBind, continuousMouseY, mouseYInvert);
            }

            // update widget graphics
            Vector2 widgetPosition = new Vector2(
                mouseXIsRelative ? relativeMouse.x / Screen.width : continuousMouseX,
                mouseYIsRelative ? relativeMouse.y / Screen.height : continuousMouseY
            );
            mouseWidget.UpdateWidgetSprites(widgetPosition);
            
            // store relative rate for relative return rate next frame
            _previousRelativeRate.x = MathfExtensions.Clamp(-Screen.width, Screen.width, relativeMouse.x);
            _previousRelativeRate.y = MathfExtensions.Clamp(-Screen.height, Screen.height, relativeMouse.y);

            // clamp to virtual screen 
            var extentsX = Screen.width * Mathf.Pow(sensitivityX, -1);
            var extentsY = Screen.height * Mathf.Pow(sensitivityY, -1);
            _mousePositionScreen.x = Math.Max(Screen.width - extentsX, Math.Min(extentsX, _mousePositionScreen.x));
            _mousePositionScreen.y = Math.Max(Screen.height - extentsY, Math.Min(extentsY, _mousePositionScreen.y));

            // we're done
            pitchMouseInput = pitch;
            rollMouseInput = roll;
            yawMouseInput = yaw;
        }
    }
}