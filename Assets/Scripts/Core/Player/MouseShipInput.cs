using GameUI;
using UnityEngine;

namespace Core.Player {
    public struct MouseInput {
        public float pitch;
        public float roll;
        public float yaw;
        public float throttle;
        public float lateralH;
        public float lateralV;
    }

    public enum MouseMode {
        Continuous,
        Relative
    }

    public class MouseShipInput : MonoBehaviour {
        [SerializeField] private InGameUI inGameUI;

        private MouseMode _mouseMode;
        private Vector2 _mousePositionDelta;
        private Vector2 _mousePositionScreen;

        private Vector2 _mouseContinuousInputPosition;
        private Vector2 _mouseRelativeInputPosition;
        private Vector2 _mousePreviousRelativeRate;

        private string _mouseXAxisBindPref;
        private string _mouseYAxisBindPref;
        private float _sensitivityXPref;
        private float _sensitivityYPref;
        private float _sensitivityXRelativeOnlyPref;
        private float _sensitivityYRelativeOnlyPref;
        private bool _mouseXInvertPref;
        private bool _mouseYInvertPref;
        private bool _mouseRelativeOnlyOnNoInput;
        private bool _mouseSeparateRelativeSensitivity;
        private float _mouseRelativeRatePref;
        private float _mouseRelativeCurvePref;
        private float _mouseDeadzonePref;
        private float _mousePowerCurvePref;

        private bool _mouseInputThisFrame;

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnSettingApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnSettingApplied;
        }

        private void FixedUpdate() {
            if (_mouseMode == MouseMode.Relative) UpdateRelativeReturnRate();
        }

        private void Update() {
            // update widget graphics
            var widgetPosition = new Vector2(
                _mouseMode == MouseMode.Relative ? _mouseRelativeInputPosition.x : _mouseContinuousInputPosition.x,
                _mouseMode == MouseMode.Relative ? _mouseRelativeInputPosition.y : _mouseContinuousInputPosition.y
            );
            inGameUI.MouseWidget.UpdateWidgetSprites(widgetPosition);
        }

        public void ResetToCentre(Vector2 mouseCenter) {
            _mousePositionScreen = mouseCenter;
            _mousePositionDelta = Vector2.zero;
            _mousePreviousRelativeRate = Vector2.zero;

            _mouseContinuousInputPosition = Vector2.zero;
            _mouseRelativeInputPosition = Vector2.zero;
            inGameUI.MouseWidget.ResetToCentre();
        }

        public void SetMouseInput(Vector2 mousePositionDelta) {
            if (mousePositionDelta.x != 0 || mousePositionDelta.y != 0) _mouseInputThisFrame = true;

            _mousePositionDelta = mousePositionDelta;
            _mousePositionScreen.x += _mousePositionDelta.x;
            _mousePositionScreen.y += _mousePositionDelta.y;
        }

        public MouseInput CalculateMouseInput(MouseMode mouseMode) {
            _mouseMode = mouseMode;

            var mouseInput = new MouseInput();
            switch (mouseMode) {
                case MouseMode.Continuous:
                    // add extra room for power curve
                    var normalizedClamp = 1 + _mouseDeadzonePref;

                    // convert to normalized screen space (center is 0,0)
                    var mouseXNormalized = (_mousePositionScreen.x / Screen.width * 2 - 1) * _sensitivityXPref;
                    var mouseYNormalized = (_mousePositionScreen.y / Screen.height * 2 - 1) * _sensitivityYPref;

                    // clamp to max extents of mouse input
                    Vector2 mousePositionNormalized;
                    mousePositionNormalized.x = Mathf.Clamp(mouseXNormalized, -normalizedClamp, normalizedClamp);
                    mousePositionNormalized.y = Mathf.Clamp(mouseYNormalized, -normalizedClamp, normalizedClamp);

                    // write back the clamped values to keep our max mouse position intact
                    _mousePositionScreen.x = (mousePositionNormalized.x / _sensitivityXPref + 1) / 2 * Screen.width;
                    _mousePositionScreen.y = (mousePositionNormalized.y / _sensitivityYPref + 1) / 2 * Screen.height;

                    CalculateMouseInputContinuous(ref mouseInput, mousePositionNormalized);
                    break;
                case MouseMode.Relative:
                    Vector2 mousePositionNormalizedDelta;
                    mousePositionNormalizedDelta.x = _mousePositionDelta.x / Screen.width;
                    mousePositionNormalizedDelta.y = _mousePositionDelta.y / Screen.height;

                    CalculateMouseInputRelative(ref mouseInput, mousePositionNormalizedDelta);
                    break;
            }

            return mouseInput;
        }

        private void CalculateMouseInputContinuous(ref MouseInput mouseInput, Vector2 mousePositionNormalized) {
            // calculate continuous input including deadzone and sensitivity
            if (mousePositionNormalized.x > _mouseDeadzonePref)
                _mouseContinuousInputPosition.x = mousePositionNormalized.x - _mouseDeadzonePref;
            if (mousePositionNormalized.x < -_mouseDeadzonePref)
                _mouseContinuousInputPosition.x = mousePositionNormalized.x + _mouseDeadzonePref;
            if (mousePositionNormalized.y > _mouseDeadzonePref)
                _mouseContinuousInputPosition.y = mousePositionNormalized.y - _mouseDeadzonePref;
            if (mousePositionNormalized.y < -_mouseDeadzonePref)
                _mouseContinuousInputPosition.y = mousePositionNormalized.y + _mouseDeadzonePref;

            _mouseRelativeInputPosition.x = (_mouseRelativeInputPosition.x < 0 ? -1 : 1) *
                                            Mathf.Pow(Mathf.Abs(_mouseRelativeInputPosition.x), _mousePowerCurvePref);
            _mouseRelativeInputPosition.y = (_mouseRelativeInputPosition.y < 0 ? -1 : 1) *
                                            Mathf.Pow(Mathf.Abs(_mouseRelativeInputPosition.y), _mousePowerCurvePref);

            // send input depending on mouse mode
            SetInput(ref mouseInput, _mouseXAxisBindPref, _mouseContinuousInputPosition.x, _mouseXInvertPref);
            SetInput(ref mouseInput, _mouseYAxisBindPref, _mouseContinuousInputPosition.y, _mouseYInvertPref);
        }


        private void CalculateMouseInputRelative(ref MouseInput mouseInput, Vector2 mousePositionNormalizedDelta) {
            var sensitivityX = _mouseSeparateRelativeSensitivity ? _sensitivityXRelativeOnlyPref : _sensitivityXPref;
            var sensitivityY = _mouseSeparateRelativeSensitivity ? _sensitivityYRelativeOnlyPref : _sensitivityYPref;

            // calculate relative input from deltas including sensitivity
            _mouseRelativeInputPosition.x =
                _mousePreviousRelativeRate.x + mousePositionNormalizedDelta.x * sensitivityX;
            _mouseRelativeInputPosition.y =
                _mousePreviousRelativeRate.y + mousePositionNormalizedDelta.y * sensitivityY;

            // store relative rate for relative return rate next frame
            _mousePreviousRelativeRate.x = Mathf.Clamp(_mouseRelativeInputPosition.x, -1, 1);
            _mousePreviousRelativeRate.y = Mathf.Clamp(_mouseRelativeInputPosition.y, -1, 1);

            // handle relative input deadzone
            if (_mouseRelativeInputPosition.x < _mouseDeadzonePref &&
                _mouseRelativeInputPosition.x > -_mouseDeadzonePref) _mouseRelativeInputPosition.x = 0;
            if (_mouseRelativeInputPosition.y < _mouseDeadzonePref &&
                _mouseRelativeInputPosition.y > -_mouseDeadzonePref) _mouseRelativeInputPosition.y = 0;

            // power curve (Mathf.Pow does not allow negatives because REASONS so abs and multiply by -1 if the original val is < 0)
            _mouseRelativeInputPosition.x = (_mouseRelativeInputPosition.x < 0 ? -1 : 1) *
                                            Mathf.Pow(Mathf.Abs(_mouseRelativeInputPosition.x), _mousePowerCurvePref);
            _mouseRelativeInputPosition.y = (_mouseRelativeInputPosition.y < 0 ? -1 : 1) *
                                            Mathf.Pow(Mathf.Abs(_mouseRelativeInputPosition.y), _mousePowerCurvePref);

            // send input depending on mouse mode
            SetInput(ref mouseInput, _mouseXAxisBindPref, _mouseRelativeInputPosition.x, _mouseXInvertPref);
            SetInput(ref mouseInput, _mouseYAxisBindPref, _mouseRelativeInputPosition.y, _mouseYInvertPref);
        }

        private void SetInput(ref MouseInput mouseInput, string axis, float amount, bool shouldInvert) {
            var invert = shouldInvert ? -1 : 1;

            switch (axis) {
                case "pitch":
                    mouseInput.pitch += amount * invert;
                    break;
                case "roll":
                    mouseInput.roll += amount * invert;
                    break;
                case "yaw":
                    mouseInput.yaw += amount * invert;
                    break;
                case "lateral h":
                    mouseInput.lateralH += amount * invert;
                    break;
                case "lateral v":
                    mouseInput.lateralV += amount * invert;
                    break;
                case "throttle":
                    mouseInput.throttle += amount * invert;
                    break;
            }
        }

        private void UpdateRelativeReturnRate() {
            if (_mouseInputThisFrame) {
                _mouseInputThisFrame = false;
                if (_mouseRelativeOnlyOnNoInput) return;
            }

            // return to 0 by mouseRelativeRate
            var distance = _mouseRelativeInputPosition.magnitude;
            var returnCurve = Mathf.Clamp(1 + distance - Mathf.Pow(distance, 1 / _mouseRelativeCurvePref), 0, 1);
            var returnRate = _mouseRelativeRatePref / 500 * returnCurve;
            _mousePreviousRelativeRate = Vector2.MoveTowards(new Vector2(
                    _mouseRelativeInputPosition.x,
                    _mouseRelativeInputPosition.y),
                Vector2.zero, returnRate);

            // store relative rate for relative return rate next frame
            _mousePreviousRelativeRate.x = Mathf.Clamp(_mousePreviousRelativeRate.x, -1, 1);
            _mousePreviousRelativeRate.y = Mathf.Clamp(_mousePreviousRelativeRate.y, -1, 1);
        }

        private void OnSettingApplied() {
            _mouseXAxisBindPref = Preferences.Instance.GetString("mouseXAxis");
            _mouseYAxisBindPref = Preferences.Instance.GetString("mouseYAxis");

            _mouseXInvertPref = Preferences.Instance.GetBool("mouseXInvert");
            _mouseYInvertPref = Preferences.Instance.GetBool("mouseYInvert");
            _mouseSeparateRelativeSensitivity = Preferences.Instance.GetBool("mouseSeparateRelativeSensitivity");

            _sensitivityXPref = Preferences.Instance.GetFloat("mouseXSensitivity");
            _sensitivityYPref = Preferences.Instance.GetFloat("mouseYSensitivity");
            _sensitivityXRelativeOnlyPref = Preferences.Instance.GetFloat("mouseXRelativeSensitivity");
            _sensitivityYRelativeOnlyPref = Preferences.Instance.GetFloat("mouseYRelativeSensitivity");


            _mouseRelativeOnlyOnNoInput = Preferences.Instance.GetBool("mouseRelativeReturnOnlyOnNoInput");
            _mouseRelativeRatePref = Mathf.Clamp(Preferences.Instance.GetFloat("mouseRelativeRate"), 1, 50);
            _mouseRelativeCurvePref = Mathf.Clamp(Preferences.Instance.GetFloat("mouseRelativeCurve"), 1, 100);

            _mouseDeadzonePref = Mathf.Clamp(Preferences.Instance.GetFloat("mouseDeadzone"), 0, 1);
            _mousePowerCurvePref = Mathf.Clamp(Preferences.Instance.GetFloat("mousePowerCurve"), 1, 3);
        }
    }
}