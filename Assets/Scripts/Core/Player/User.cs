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
        [SerializeField] public bool pauseMenuEnabled = true;
        [SerializeField] public bool boostButtonEnabledOverride;
        private bool _alternateFlightControls;
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
            if (movementEnabled) {
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
                    CalculateMouseInput(out var mousePitch, out var mouseRoll, out var mouseYaw);
                    pitch += mousePitch;
                    roll += mouseRoll;
                    yaw += mouseYaw;
                }

                // update the player
                if (Preferences.Instance.GetBool("autoShipRotation") || Preferences.Instance.GetString("controlSchemeType") == "arcade")
                    shipArcadeFlightComputer.UpdateShipFlightInput(ref lateralH, ref lateralV, ref throttle, ref pitch, ref yaw, ref roll);

                shipPlayer.SetLateralH(lateralH);
                shipPlayer.SetLateralV(lateralV);
                shipPlayer.SetThrottle(throttle);
                shipPlayer.SetPitch(pitch);
                shipPlayer.SetYaw(yaw);
                shipPlayer.SetRoll(roll);
                shipPlayer.Boost(_boost);
                shipPlayer.VelocityLimiterIsPressed(_limiter);

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

            if (boostButtonEnabledOverride) shipPlayer.Boost(_boost);
        }

        public void OnEnable() {
            _mouseLookActive = Preferences.Instance.GetString("controlSchemeType") == "advanced" && Preferences.Instance.GetBool("mouseLook");
            Game.OnVRStatus += SetVRStatus;
            ResetMouseToCentre();
            FdConsole.Instance.Clear();
        }

        public void OnDisable() {
            Game.OnVRStatus -= SetVRStatus;
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

            // enable multiple input action sets
            playerInput.actions.FindActionMap("Global").Enable();
            playerInput.currentActionMap.Enable();

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
            foreach (var playerInputDevice in playerInput.devices) FdConsole.Instance.LogMessage(playerInputDevice.name + " paired");

            ResetMouseToCentre();
        }

        public void DisableGameInput() {
            var playerInput = GetComponent<PlayerInput>();

            // enable multiple input action sets
            playerInput.actions.FindActionMap("Ship").Disable();
            playerInput.actions.FindActionMap("ShipArcade").Disable();

            movementEnabled = false;
            boostButtonEnabledOverride = false;

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
            inGameUI.MouseWidgetScreen.ResetToCentre();
            inGameUI.MouseWidgetWorld.ResetToCentre();
        }

        // Event responders for PlayerInput, only valid in-game.
        [UsedImplicitly]
        public void OnShowGameMenu() {
            if (pauseMenuEnabled) inGameUI.OnGameMenuToggle();
        }

        [UsedImplicitly]
        public void OnRestartTrack() {
            if (movementEnabled) Game.Instance.RestartSession();
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
        public void OnVelocityLimiter(InputValue value) {
            _limiter = value.isPressed;
        }

        [UsedImplicitly]
        public void OnAllFlightAssistToggle(InputValue value) {
            shipPlayer.AllFlightAssistToggle();
        }

        [UsedImplicitly]
        public void OnVectorFlightAssistToggle(InputValue value) {
            shipPlayer.FlightAssistVectorControlToggle();
        }

        [UsedImplicitly]
        public void OnRotationalFlightAssistToggle(InputValue value) {
            shipPlayer.FlightAssistRotationalDampeningToggle();
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
            _mousePositionNormalized = new Vector2(
                _mousePositionScreen.x / Screen.width * 2 - 1,
                _mousePositionScreen.y / Screen.height * 2 - 1
            );
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
                Debug.Log("HI FREECAM");
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

        private void CalculateMouseInput(out float pitchMouseInput, out float rollMouseInput, out float yawMouseInput) {
            float pitch = 0, roll = 0, yaw = 0;

            var mouseXAxisBind = Preferences.Instance.GetString("mouseXAxis");
            var mouseYAxisBind = Preferences.Instance.GetString("mouseYAxis");

            var sensitivityX = Preferences.Instance.GetFloat("mouseXSensitivity");
            var sensitivityY = Preferences.Instance.GetFloat("mouseYSensitivity");

            var mouseXInvert = Preferences.Instance.GetBool("mouseXInvert");
            var mouseYInvert = Preferences.Instance.GetBool("mouseXInvert");

            var mouseXIsRelative = Preferences.Instance.GetBool("relativeMouseXAxis") ||
                                   Preferences.Instance.GetBool("forceRelativeMouseWithFAOff") && !shipPlayer.IsRotationalFlightAssistActive;
            var mouseYIsRelative = Preferences.Instance.GetBool("relativeMouseYAxis") ||
                                   Preferences.Instance.GetBool("forceRelativeMouseWithFAOff") && !shipPlayer.IsRotationalFlightAssistActive;

            var mouseRelativeRate = Mathf.Clamp(Preferences.Instance.GetFloat("mouseRelativeRate"), 1, 50f);

            var mouseDeadzone = Mathf.Clamp(Preferences.Instance.GetFloat("mouseDeadzone"), 0, 1);
            var mousePowerCurve = Mathf.Clamp(Preferences.Instance.GetFloat("mousePowerCurve"), 1, 3);

            // // get deadzone as a pixel value including sensitivity change
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
                        pitch += amount * invert;
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
            setInput(mouseXAxisBind, mouseXIsRelative ? relativeMouse.x : continuousMouseX, mouseXInvert);
            setInput(mouseYAxisBind, mouseYIsRelative ? relativeMouse.y : continuousMouseY, mouseYInvert);

            // update widget graphics
            var widgetPosition = new Vector2(
                mouseXIsRelative ? relativeMouse.x / Screen.width : continuousMouseX,
                mouseYIsRelative ? relativeMouse.y / Screen.height : continuousMouseY
            );
            inGameUI.MouseWidgetWorld.UpdateWidgetSprites(widgetPosition);
            inGameUI.MouseWidgetScreen.UpdateWidgetSprites(widgetPosition);

            // store relative rate for relative return rate next frame
            _previousRelativeRate.x = Mathf.Clamp(relativeMouse.x, -Screen.width, Screen.width);
            _previousRelativeRate.y = Mathf.Clamp(relativeMouse.y, -Screen.height, Screen.height);

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