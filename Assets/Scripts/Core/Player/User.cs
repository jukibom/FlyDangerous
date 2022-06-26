using System;
using System.Linq;
using Audio;
using GameUI;
using JetBrains.Annotations;
using Misc;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.XR.Interaction.Toolkit;

namespace Core.Player {
    public class User : MonoBehaviour {
        [SerializeField] private ShipPlayer shipPlayer;
        [SerializeField] private ShipArcadeFlightComputer shipArcadeFlightComputer;
        [SerializeField] private ShipCameraRig shipCameraRig;
        [SerializeField] private XRRig xrRig;
        [SerializeField] private InputSystemUIInputModule pauseUIInputModule;
        [SerializeField] public InGameUI inGameUI;

        [SerializeField] public bool movementEnabled;
        [SerializeField] public bool restartEnabled = true;
        [SerializeField] public bool pauseMenuEnabled = true;
        [SerializeField] public bool boostButtonForceEnabled;
        private bool _alternateFlightControls;
        private bool _autoRotateDrift;
        private bool _boost;
        private Vector2 _cameraMouse;
        private bool _cameraRotateAxisControlsEnabled = true;

        private float _cameraX;
        private float _cameraY;

        private bool _freeCamEnabled;

        private float _lateralH;
        private float _lateralV;
        private bool _limiter;
        private bool _mouseLookActive;
        private Vector2 _mousePositionDelta;
        private Vector2 _mousePositionNormalized;
        private Vector2 _mousePositionNormalizedDelta;
        private Vector2 _mousePositionScreen;
        private float _pitch;
        private Vector2 _previousRelativeRate;
        private bool _reverse;
        private float _roll;
        private float _targetThrottle;
        private float _targetThrottleIncrement;
        private float _throttle;
        private float _yaw;

        public InGameUI InGameUI => inGameUI;

        public Vector3 UserCameraPosition =>
            Game.Instance.IsVREnabled
                ? xrRig.cameraGameObject.transform.position
                : shipCameraRig.ActiveCamera.transform.position;

        public ShipCameraRig ShipCameraRig => shipCameraRig;

        public void Awake() {
            DisableGameInput();
            DisableUIInput();
            ResetMouseToCentre();
        }

        public void Update() {
            if (movementEnabled && !shipPlayer.Freeze) {
                var pitch = _pitch;
                var roll = _roll;
                var yaw = _yaw;
                var throttle = _throttle;
                var lateralH = _lateralH;
                var lateralV = _lateralV;

                // handle advanced throttle control
                if (Preferences.Instance.GetString("throttleType") == "forward only") {
                    throttle = MathfExtensions.Remap(-1, 1, 0, 1, throttle);
                    if (_reverse) throttle *= -1;
                }

                _targetThrottle = Mathf.Clamp(_targetThrottle + _targetThrottleIncrement, -1, 1);
                if (_targetThrottle != 0) throttle = _targetThrottle;

                // handle mouse flight input
                if (
                    !inGameUI.PauseSystem.IsPaused &&
                    !_mouseLookActive &&
                    Preferences.Instance.GetBool("enableMouseFlightControls") &&
                    Preferences.Instance.GetString("controlSchemeType") == "advanced"
                ) {
                    CalculateMouseInput(
                        out var mousePitch, out var mouseRoll, out var mouseYaw, out var mouseLateralH, out var mouseLateralV, out var mouseThrottle);
                    pitch += mousePitch;
                    roll += mouseRoll;
                    yaw += mouseYaw;
                    lateralH += mouseLateralH;
                    lateralV += mouseLateralV;
                    throttle += mouseThrottle;
                }

                // if user has any auto-roll handling set, invoke the arcade flight computer to override the inputs
                if (Preferences.Instance.GetBool("autoShipRoll") ||
                    Preferences.Instance.GetBool("autoShipRotation") ||
                    Preferences.Instance.GetString("controlSchemeType") == "arcade")
                    shipArcadeFlightComputer.UpdateShipFlightInput(ref lateralH, ref lateralV, ref throttle, ref pitch, ref yaw, ref roll, _autoRotateDrift);

                // update the player
                shipPlayer.SetPitch(pitch);
                shipPlayer.SetYaw(yaw);
                shipPlayer.SetRoll(roll);
                shipPlayer.SetLateralH(lateralH);
                shipPlayer.SetLateralV(lateralV);
                shipPlayer.SetThrottle(throttle);
                shipPlayer.VelocityLimiterIsPressed(_limiter);
                shipPlayer.Boost(_boost);

                // handle camera rig
                if (_cameraRotateAxisControlsEnabled) {
                    if ((Preferences.Instance.GetString("cameraMode") == "absolute" ||
                         Preferences.Instance.GetString("controlSchemeType") == "arcade") &&
                        !_mouseLookActive)
                        shipCameraRig.SetPosition(new Vector2(_cameraX, _cameraY), CameraPositionUpdate.Absolute);

                    else if (Preferences.Instance.GetString("cameraMode") == "relative" || _mouseLookActive)
                        shipCameraRig.SetPosition(new Vector2(_cameraX, _cameraY), CameraPositionUpdate.Relative);
                }
                else {
                    shipCameraRig.SetPosition(Vector2.zero, CameraPositionUpdate.Absolute);
                }

                if (_mouseLookActive)
                    shipCameraRig.SetPosition(
                        new Vector2(
                            _mousePositionDelta.x / Screen.width * Preferences.Instance.GetFloat("mouseXSensitivity") * 100,
                            _mousePositionDelta.y / Screen.height * Preferences.Instance.GetFloat("mouseYSensitivity") * 100
                        ),
                        CameraPositionUpdate.Relative
                    );
            }

            if (boostButtonForceEnabled) shipPlayer.Boost(_boost);
        }

        public void OnEnable() {
            _mouseLookActive = Preferences.Instance.GetString("controlSchemeType") == "advanced" && Preferences.Instance.GetBool("mouseLook");
            Game.OnVRStatus += SetVRStatus;
            InputSystem.onDeviceChange += OnDeviceChange;
            ResetMouseToCentre();
            FdConsole.Instance.Clear();
        }

        public void OnDisable() {
            Game.OnVRStatus -= SetVRStatus;
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        /**
         * Enable and Disable input modules depending on context (in-game vs UI).
         * This prevents conflicts between the two, especially when rebinding keys...
         */
        public void EnableGameInput() {
            var playerInput = GetComponent<PlayerInput>();
            playerInput.ActivateInput();

            // choose the correct action set depending on advanced control scheme preference
            if (Preferences.Instance.GetString("controlSchemeType") == "advanced") {
                var advancedControlActionMap = playerInput.actions.FindActionMap("Ship");
                playerInput.currentActionMap = advancedControlActionMap ?? playerInput.currentActionMap;
            }
            else {
                var arcadeActionMap = playerInput.actions.FindActionMap("ShipArcade");
                playerInput.currentActionMap = arcadeActionMap ?? playerInput.currentActionMap;
            }

            // ensure that the global action set of the control scheme type is enabled
            playerInput.actions.FindActionMap(Preferences.Instance.GetString("controlSchemeType") == "arcade" ? "GlobalArcade" : "Global").Enable();
            playerInput.actions.FindActionMap(Preferences.Instance.GetString("controlSchemeType") == "advanced" ? "GlobalArcade" : "Global").Disable();

            playerInput.currentActionMap.Enable();

            movementEnabled = true;
            pauseMenuEnabled = true;
            restartEnabled = true;

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
            foreach (var playerInputDevice in playerInput.devices) FdConsole.Instance.LogMessage(playerInputDevice.name + " paired");

            ResetMouseToCentre();
        }

        public void DisableGameInput() {
            var playerInput = GetComponent<PlayerInput>();

            // enable multiple input action sets
            playerInput.actions.FindActionMap("Ship").Disable();
            playerInput.actions.FindActionMap("ShipArcade").Disable();

            movementEnabled = false;
            restartEnabled = false;

            // clear inputs
            shipPlayer.SetPitch(0);
            shipPlayer.SetRoll(0);
            shipPlayer.SetYaw(0);
            shipPlayer.SetThrottle(0);
            shipPlayer.SetLateralH(0);
            shipPlayer.SetLateralV(0);
            shipPlayer.Boost(false);

            FdConsole.Instance.LogMessage("** USER INPUT DISABLED **");
        }

        public void EnableUIInput() {
            pauseUIInputModule.enabled = true;
            FdConsole.Instance.LogMessage("** UI INPUT ENABLED **");
        }

        public void DisableUIInput() {
            pauseUIInputModule.enabled = false;
            FdConsole.Instance.LogMessage("** UI INPUT DISABLED **");
        }

        public void EnableFreeCamInput() {
            var playerInput = GetComponent<PlayerInput>();
            playerInput.actions.FindActionMap("FreeCam").Enable();
            FdConsole.Instance.LogMessage("** FREE CAM ENABLED **");
        }

        public void DisableFreeCamInput() {
            var playerInput = GetComponent<PlayerInput>();
            playerInput.actions.FindActionMap("FreeCam").Disable();
            FdConsole.Instance.LogMessage("** FREE CAM DISABLED **");
        }

        public void SetVRStatus(bool isVREnabled) {
            if (isVREnabled) {
                Game.Instance.SetFlatScreenCameraControllerActive(false);
                shipCameraRig.gameObject.SetActive(false);
                xrRig.gameObject.SetActive(true);
            }
            else {
                Game.Instance.SetFlatScreenCameraControllerActive(true);
                shipCameraRig.gameObject.SetActive(true);
                xrRig.gameObject.SetActive(false);
            }

            // if VR is enabled, we need to swap our active cameras and make UI panels operate in world space
            inGameUI.SetMode(isVREnabled ? GameUIMode.VR : GameUIMode.Pancake);
        }

        public void ResetMouseToCentre() {
            var warpedPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Mouse.current.WarpCursorPosition(warpedPosition);
            InputState.Change(Mouse.current.position, warpedPosition);
            _mousePositionScreen = warpedPosition;
            _mousePositionNormalized = new Vector2(0, 0);
            _mousePositionDelta = new Vector2(0, 0);
            inGameUI.MouseWidget.ResetToCentre();
        }

        // Event responders for PlayerInput, only valid in-game.
        [UsedImplicitly]
        public void OnShowGameMenu() {
            if (pauseMenuEnabled) inGameUI.OnGameMenuToggle();
        }

        [UsedImplicitly]
        public void OnRestartTrack() {
            if (restartEnabled) Game.Instance.RestartSession();
        }

        [UsedImplicitly]
        public void OnRestartFromLastCheckpoint() {
            if (movementEnabled) Debug.Log("Lol there are no checkpoints yet ^_^");
        }

        [UsedImplicitly]
        public void OnPitch(InputValue value) {
            if (!_alternateFlightControls) _pitch = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnPitchAlt(InputValue value) {
            if (_alternateFlightControls) _pitch = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnRoll(InputValue value) {
            if (!_alternateFlightControls) _roll = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnRollAlt(InputValue value) {
            if (_alternateFlightControls) _roll = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnYaw(InputValue value) {
            if (!_alternateFlightControls) _yaw = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnYawAlt(InputValue value) {
            if (_alternateFlightControls) _yaw = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnThrottle(InputValue value) {
            _targetThrottle = 0;
            _targetThrottleIncrement = 0;
            if (!_alternateFlightControls) _throttle = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnThrottleAlt(InputValue value) {
            _targetThrottle = 0;
            _targetThrottleIncrement = 0;
            if (_alternateFlightControls) _throttle = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnThrottleIncrease(InputValue value) {
            if (value.isPressed)
                _targetThrottleIncrement = MathfExtensions.Remap(0, 1, 0, 0.005f, value.Get<float>());
            else
                _targetThrottleIncrement = 0;
        }

        [UsedImplicitly]
        public void OnThrottleDecrease(InputValue value) {
            if (value.isPressed)
                _targetThrottleIncrement = MathfExtensions.Remap(0, 1, 0, -0.005f, value.Get<float>());
            else
                _targetThrottleIncrement = 0;
        }

        [UsedImplicitly]
        public void OnLateralH(InputValue value) {
            if (!_alternateFlightControls) _lateralH = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnLateralHAlt(InputValue value) {
            if (_alternateFlightControls) _lateralH = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnLateralV(InputValue value) {
            if (!_alternateFlightControls) _lateralV = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnLateralVAlt(InputValue value) {
            if (_alternateFlightControls) _lateralV = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnToggleReverse(InputValue value) {
            UIAudioManager.Instance.Play("ui-nav");
            _reverse = !_reverse;
        }

        [UsedImplicitly]
        public void OnBoost(InputValue value) {
            _boost = value.isPressed;
        }

        [UsedImplicitly]
        public void OnDrift(InputValue value) {
            _autoRotateDrift = value.isPressed;
            shipPlayer.IsAutoRotateDriftEnabled = _autoRotateDrift;
        }

        [UsedImplicitly]
        public void OnVelocityLimiter(InputValue value) {
            _limiter = value.isPressed;
        }

        [UsedImplicitly]
        public void OnAllFlightAssistToggle(InputValue value) {
            if (Preferences.Instance.GetString("flightAssistAllBindType") == "toggle" && !value.isPressed) return;

            // if any flight assist is enabled, deactivate (any on = all off)
            var isEnabled = !(shipPlayer.IsVectorFlightAssistActive | shipPlayer.IsRotationalFlightAssistActive);

            // if user has all flight assists on by default, flip that logic on its head (any off = all on)
            if (Preferences.Instance.GetString("flightAssistDefault") == "all on")
                isEnabled = !(shipPlayer.IsVectorFlightAssistActive & shipPlayer.IsRotationalFlightAssistActive);


            shipPlayer.SetAllFlightAssistEnabled(isEnabled);
        }

        [UsedImplicitly]
        public void OnVectorFlightAssistToggle(InputValue value) {
            if (Preferences.Instance.GetString("flightAssistVectorBindType") == "toggle" && !value.isPressed) return;
            shipPlayer.SetFlightAssistVectorControlEnabled(!shipPlayer.IsVectorFlightAssistActive);
        }

        [UsedImplicitly]
        public void OnRotationalFlightAssistToggle(InputValue value) {
            if (Preferences.Instance.GetString("flightAssistRotationalBindType") == "toggle" && !value.isPressed) return;
            shipPlayer.SetFlightAssistRotationalDampeningEnabled(!shipPlayer.IsRotationalFlightAssistActive);
        }

        [UsedImplicitly]
        public void OnShipLightsToggle(InputValue value) {
            shipPlayer.ShipLightsToggle();
        }

        [UsedImplicitly]
        public void OnChangeCamera(InputValue value) {
            shipCameraRig.ToggleActiveCamera();
        }

        [UsedImplicitly]
        public void OnRotateCameraH(InputValue value) {
            _cameraX = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnRotateCameraV(InputValue value) {
            _cameraY = value.Get<float>();
        }

        [UsedImplicitly]
        public void OnToggleCameraRotateControls(InputValue value) {
            _cameraRotateAxisControlsEnabled = !_cameraRotateAxisControlsEnabled;
        }

        [UsedImplicitly]
        public void OnAltFlightControlsToggle(InputValue value) {
            _pitch = 0;
            _roll = 0;
            _yaw = 0;
            _throttle = 0;
            _lateralH = 0;
            _lateralV = 0;
            _alternateFlightControls = !_alternateFlightControls;
            if (_alternateFlightControls)
                UIAudioManager.Instance.Play("ship-alternate-flight-on");
            else
                UIAudioManager.Instance.Play("ship-alternate-flight-off");
        }

        [UsedImplicitly]
        public void OnMouseRawDelta(InputValue value) {
            _mousePositionDelta = value.Get<Vector2>();
            _mousePositionScreen.x += _mousePositionDelta.x;
            _mousePositionScreen.y += _mousePositionDelta.y;
            _mousePositionNormalized.x = _mousePositionScreen.x / Screen.width * 2 - 1;
            _mousePositionNormalized.y = _mousePositionScreen.y / Screen.height * 2 - 1;
            _mousePositionNormalizedDelta.x = _mousePositionDelta.x / Screen.width;
            _mousePositionNormalizedDelta.y = _mousePositionDelta.y / Screen.height;
        }

        [UsedImplicitly]
        public void OnMouselook(InputValue value) {
            var mouseLookType = Preferences.Instance.GetString("mouseLookBindType");
            if (mouseLookType == "toggle" && value.isPressed) {
                _mouseLookActive = !_mouseLookActive;
                shipCameraRig.SoftReset();
                Preferences.Instance.SetBool("mouseLook", _mouseLookActive);
            }

            if (mouseLookType == "hold") _mouseLookActive = value.isPressed;
        }

        [UsedImplicitly]
        public void OnRecenterMouse(InputValue value) {
            ResetMouseToCentre();
        }

        [UsedImplicitly]
        public void OnResetHMDView(InputValue value) {
            if (xrRig) Game.Instance.ResetHmdView(xrRig, transform);
        }

        [UsedImplicitly]
        public void OnToggleConsole(InputValue value) {
            if (FdConsole.Instance.Visible)
                FdConsole.Instance.Hide();
            else
                FdConsole.Instance.Show();
        }

        [UsedImplicitly]
        public void OnToggleFreeCam(InputValue value) {
            if (!Game.Instance.IsVREnabled) {
                _freeCamEnabled = !_freeCamEnabled;
                if (_freeCamEnabled) {
                    DisableGameInput();
                    EnableFreeCamInput();
                    pauseMenuEnabled = false;
                }
                else {
                    EnableGameInput();
                    DisableFreeCamInput();
                }

                shipCameraRig.SetFreeCameraEnabled(_freeCamEnabled);
            }
        }

        [UsedImplicitly]
        public void OnFreeCamMove(InputValue value) {
            ShipCameraRig.ShipFreeCamera.Move(value.Get<Vector2>());
        }

        [UsedImplicitly]
        public void OnFreeCamLook(InputValue value) {
            ShipCameraRig.ShipFreeCamera.LookAround(value.Get<Vector2>());
        }

        [UsedImplicitly]
        public void OnFreeCamAscend(InputValue value) {
            ShipCameraRig.ShipFreeCamera.Ascend(value.Get<float>());
        }

        [UsedImplicitly]
        public void OnFreeCamToggleMovementLock(InputValue value) {
            Debug.Log("Movement Lock not yet implemented");
        }

        [UsedImplicitly]
        public void OnFreeCamToggleFocusLock(InputValue value) {
            if (value.isPressed) ShipCameraRig.ShipFreeCamera.ToggleAimLock();
        }

        [UsedImplicitly]
        public void OnFreeCamToggleFreeze(InputValue value) {
            if (value.isPressed) Time.timeScale = Time.timeScale != 0 ? 0 : 1;
        }

        [UsedImplicitly]
        public void OnFreeCamFieldOfView(InputValue value) {
            ShipCameraRig.ShipFreeCamera.Zoom(value.Get<float>());
        }

        [UsedImplicitly]
        public void OnFreeCamSetMotionMultiplier(InputValue value) {
            var input = value.Get<float>();

            if (input > 0) input = 1;
            if (input < 0) input = -1;
            if (input != 0) ShipCameraRig.ShipFreeCamera.IncrementMotionMultiplier(input);
        }

        private void CalculateMouseInput(out float pitchMouseInput, out float rollMouseInput, out float yawMouseInput,
            out float lateralHMouseInput, out float lateralVMouseInput, out float throttleMouseInput) {
            float pitch = 0, roll = 0, yaw = 0, throttle = 0, lateralH = 0, lateralV = 0;

            var mouseXAxisBind = Preferences.Instance.GetString("mouseXAxis");
            var mouseYAxisBind = Preferences.Instance.GetString("mouseYAxis");

            var sensitivityX = Preferences.Instance.GetFloat("mouseXSensitivity");
            var sensitivityY = Preferences.Instance.GetFloat("mouseYSensitivity");

            var mouseXInvert = Preferences.Instance.GetBool("mouseXInvert");
            var mouseYInvert = Preferences.Instance.GetBool("mouseYInvert");

            var mouseIsRelative = Preferences.Instance.GetString("mouseInputMode") == "relative" ||
                                  Preferences.Instance.GetBool("forceRelativeMouseWithFAOff") && !shipPlayer.IsRotationalFlightAssistActive;

            var mouseRelativeRate = Mathf.Clamp(Preferences.Instance.GetFloat("mouseRelativeRate"), 1, 50f);

            var mouseDeadzone = Mathf.Clamp(Preferences.Instance.GetFloat("mouseDeadzone"), 0, 1);
            var mousePowerCurve = Mathf.Clamp(Preferences.Instance.GetFloat("mousePowerCurve"), 1, 3);

            // get deadzone as a pixel value including sensitivity change
            var mouseDeadzoneX = mouseDeadzone * Mathf.Pow(sensitivityX, -1);
            var mouseDeadzoneY = mouseDeadzone * Mathf.Pow(sensitivityY, -1);

            // calculate continuous input including deadzone and sensitivity
            var continuousMouseX = 0f;
            var continuousMouseY = 0f;
            if (_mousePositionNormalized.x > mouseDeadzoneX) continuousMouseX = (_mousePositionNormalized.x - mouseDeadzone) * sensitivityX;
            if (_mousePositionNormalized.x < -mouseDeadzoneX) continuousMouseX = (_mousePositionNormalized.x + mouseDeadzone) * sensitivityX;
            if (_mousePositionNormalized.y > mouseDeadzoneY) continuousMouseY = (_mousePositionNormalized.y - mouseDeadzone) * sensitivityY;
            if (_mousePositionNormalized.y < -mouseDeadzoneY) continuousMouseY = (_mousePositionNormalized.y + mouseDeadzone) * sensitivityY;

            // calculate relative input from deltas including sensitivity
            var relativeMouse = new Vector2(_mousePositionNormalizedDelta.x * sensitivityX, _mousePositionNormalizedDelta.y * sensitivityY);

            // return to 0 by mouseRelativeRate
            relativeMouse += Vector2.MoveTowards(new Vector2(
                    _previousRelativeRate.x,
                    _previousRelativeRate.y),
                Vector2.zero, mouseRelativeRate / 500);

            // store relative rate for relative return rate next frame
            _previousRelativeRate.x = Mathf.Clamp(relativeMouse.x, -1, 1);
            _previousRelativeRate.y = Mathf.Clamp(relativeMouse.y, -1, 1);

            // power curve (Mathf.Pow does not allow negatives because REASONS so abs and multiply by -1 if the original val is < 0)
            continuousMouseX = (continuousMouseX < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(continuousMouseX), mousePowerCurve);
            continuousMouseY = (continuousMouseY < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(continuousMouseY), mousePowerCurve);
            relativeMouse.x = (relativeMouse.x < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(relativeMouse.x), mousePowerCurve);
            relativeMouse.y = (relativeMouse.y < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(relativeMouse.y), mousePowerCurve);

            // set the input for a given axis 
            void SetInput(string axis, float amount, bool shouldInvert) {
                var invert = shouldInvert ? -1 : 1;

                switch (axis) {
                    case "pitch":
                        pitch += amount * invert;
                        break;
                    case "roll":
                        roll += amount * invert;
                        break;
                    case "yaw":
                        yaw += amount * invert;
                        break;
                    case "lateral h":
                        lateralH += amount * invert;
                        break;
                    case "lateral v":
                        lateralV += amount * invert;
                        break;
                    case "throttle":
                        throttle += amount * invert;
                        break;
                }
            }

            // send input depending on mouse mode
            SetInput(mouseXAxisBind, mouseIsRelative ? relativeMouse.x : continuousMouseX, mouseXInvert);
            SetInput(mouseYAxisBind, mouseIsRelative ? relativeMouse.y : continuousMouseY, mouseYInvert);

            // update widget graphics
            var widgetPosition = new Vector2(
                mouseIsRelative ? relativeMouse.x : continuousMouseX,
                mouseIsRelative ? relativeMouse.y : continuousMouseY
            );
            inGameUI.MouseWidget.UpdateWidgetSprites(widgetPosition);

            // clamp to virtual screen 
            var extentsX = Screen.width * Mathf.Pow(sensitivityX, -1);
            var extentsY = Screen.height * Mathf.Pow(sensitivityY, -1);
            _mousePositionScreen.x = Math.Max(Screen.width - extentsX, Math.Min(extentsX, _mousePositionScreen.x));
            _mousePositionScreen.y = Math.Max(Screen.height - extentsY, Math.Min(extentsY, _mousePositionScreen.y));

            // we're done
            pitchMouseInput = pitch;
            rollMouseInput = roll;
            yawMouseInput = yaw;
            lateralHMouseInput = lateralH;
            lateralVMouseInput = lateralV;
            throttleMouseInput = throttle;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change) {
            var playerInput = GetComponent<PlayerInput>();
            if (change == InputDeviceChange.Added) InputUser.PerformPairingWithDevice(device, playerInput.user);
            if (change == InputDeviceChange.Removed) playerInput.user.UnpairDevice(device);
        }
    }
}