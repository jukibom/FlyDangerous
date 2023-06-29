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

        private Vector2 _positionDelta;
        private Vector2 _positionScreen;

        // private Vector2 _continuousInput;
        private Vector2 _widgetPosition;
        private Vector2 _widgetInputActual;
        private Vector2 _currentRelativeInput;
        private Vector2 _previousRelativeInput;

        private string _xAxisBindPref;
        private string _yAxisBindPref;
        private float _sensitivityXPref;
        private float _sensitivityYPref;
        private float _sensitivityXRelativeOnlyPref;
        private float _sensitivityYRelativeOnlyPref;
        private bool _xInvertPref;
        private bool _yInvertPref;
        private bool _relativeOnlyOnNoInput;
        private bool _separateRelativeSensitivity;
        private float _relativeRatePref;
        private float _relativeCurvePref;
        private float _deadzonePref;
        private float _powerCurvePref;

        private bool _wasInputThisFrame;

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnSettingApplied;
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnSettingApplied;
        }

        private void FixedUpdate() {
            UpdateRelativeReturnRate();
        }

        private void Update() {
            // update widget graphics
            inGameUI.MouseWidget.UpdateWidgetSprites(_widgetPosition, _widgetInputActual);
        }

        public void ResetToCentre(Vector2 mouseCenter) {
            _positionScreen = mouseCenter;
            _positionDelta = Vector2.zero;
            _previousRelativeInput = Vector2.zero;

            _widgetPosition = Vector2.zero;
            _widgetInputActual = Vector2.zero;
            inGameUI.MouseWidget.ResetToCentre();
        }

        public void SetMouseInput(Vector2 mousePositionDelta) {
            if (mousePositionDelta.x != 0 || mousePositionDelta.y != 0) _wasInputThisFrame = true;

            _positionDelta = mousePositionDelta;
            _positionScreen.x += _positionDelta.x;
            _positionScreen.y += _positionDelta.y;
        }

        public MouseInput CalculateMouseInput(MouseMode mouseMode) {
            var mouseInput = new MouseInput();
            switch (mouseMode) {
                case MouseMode.Continuous:
                    // convert to normalized screen space (center is 0,0)
                    var xNormalized = (_positionScreen.x / Screen.width * 2 - 1) * _sensitivityXPref;
                    var yNormalized = (_positionScreen.y / Screen.height * 2 - 1) * _sensitivityYPref;

                    // clamp to max extents of mouse input
                    Vector2 positionNormalized;
                    positionNormalized.x = Mathf.Clamp(xNormalized, -1, 1);
                    positionNormalized.y = Mathf.Clamp(yNormalized, -1, 1);

                    // write back the clamped values to keep our max mouse position intact
                    _positionScreen.x = (positionNormalized.x / _sensitivityXPref + 1) / 2 * Screen.width;
                    _positionScreen.y = (positionNormalized.y / _sensitivityYPref + 1) / 2 * Screen.height;

                    CalculateMouseInputContinuous(ref mouseInput, positionNormalized);
                    break;
                case MouseMode.Relative:
                    Vector2 positionNormalizedDelta;
                    positionNormalizedDelta.x = _positionDelta.x / Screen.width;
                    positionNormalizedDelta.y = _positionDelta.y / Screen.height;

                    CalculateMouseInputRelative(ref mouseInput, positionNormalizedDelta);
                    break;
            }

            return mouseInput;
        }

        private void CalculateMouseInputContinuous(ref MouseInput mouseInput, Vector2 positionNormalized) {
            _widgetPosition = positionNormalized;
            var input = positionNormalized;

            // deadzone
            if (input.magnitude < _deadzonePref) input = Vector2.zero;
            else input = input * (input.magnitude - _deadzonePref) / (1 - _deadzonePref);

            // power curve
            input.x = (input.x < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(input.x), _powerCurvePref);
            input.y = (input.y < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(input.y), _powerCurvePref);

            // clamp input
            input.x = Mathf.Clamp(input.x, -1, 1);
            input.y = Mathf.Clamp(input.y, -1, 1);

            // send input
            SetInput(ref mouseInput, _xAxisBindPref, input.x, _xInvertPref);
            SetInput(ref mouseInput, _yAxisBindPref, input.y, _yInvertPref);
            _widgetInputActual = input;
        }

        private void CalculateMouseInputRelative(ref MouseInput mouseInput, Vector2 positionNormalizedDelta) {
            var sensitivityX = _separateRelativeSensitivity ? _sensitivityXRelativeOnlyPref : _sensitivityXPref;
            var sensitivityY = _separateRelativeSensitivity ? _sensitivityYRelativeOnlyPref : _sensitivityYPref;

            // get relative input from deltas including sensitivity
            var delta = new Vector2(positionNormalizedDelta.x * sensitivityX, positionNormalizedDelta.y * sensitivityY);

            // calculate relative input from deltas and previous input
            _currentRelativeInput.x = _previousRelativeInput.x + delta.x;
            _currentRelativeInput.y = _previousRelativeInput.y + delta.y;

            var input = _currentRelativeInput;
            _widgetPosition = input;

            // deadzone
            if (input.magnitude < _deadzonePref) input = Vector2.zero;
            else input = input * (input.magnitude - _deadzonePref) / (1 - _deadzonePref);

            // power curve
            input.x = (input.x < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(input.x), _powerCurvePref);
            input.y = (input.y < 0 ? -1 : 1) * Mathf.Pow(Mathf.Abs(input.y), _powerCurvePref);

            // store relative rate for next frame
            _previousRelativeInput.x = Mathf.Clamp(_currentRelativeInput.x, -1, 1);
            _previousRelativeInput.y = Mathf.Clamp(_currentRelativeInput.y, -1, 1);

            // clamp input
            input.x = Mathf.Clamp(input.x, -1, 1);
            input.y = Mathf.Clamp(input.y, -1, 1);

            // send input
            SetInput(ref mouseInput, _xAxisBindPref, input.x, _xInvertPref);
            SetInput(ref mouseInput, _yAxisBindPref, input.y, _yInvertPref);
            _widgetInputActual = input;
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
            if (_wasInputThisFrame) {
                _wasInputThisFrame = false;
                if (_relativeOnlyOnNoInput) return;
            }

            // return to 0 by mouseRelativeRate
            var distance = _previousRelativeInput.magnitude;
            var returnCurve = Mathf.Clamp(1 + distance - Mathf.Pow(distance, 1 / _relativeCurvePref), 0, 1);
            var returnRate = _relativeRatePref / 500 * returnCurve;
            _previousRelativeInput =
                Vector2.MoveTowards(new Vector2(_currentRelativeInput.x, _currentRelativeInput.y), Vector2.zero,
                    returnRate);
        }

        private void OnSettingApplied() {
            _xAxisBindPref = Preferences.Instance.GetString("mouseXAxis");
            _yAxisBindPref = Preferences.Instance.GetString("mouseYAxis");

            _xInvertPref = Preferences.Instance.GetBool("mouseXInvert");
            _yInvertPref = Preferences.Instance.GetBool("mouseYInvert");
            _separateRelativeSensitivity = Preferences.Instance.GetBool("mouseSeparateRelativeSensitivity");

            _sensitivityXPref = Mathf.Clamp(Preferences.Instance.GetFloat("mouseXSensitivity"), 0.5f, 5f);
            _sensitivityYPref = Mathf.Clamp(Preferences.Instance.GetFloat("mouseYSensitivity"), 0.5f, 5f);
            _sensitivityXRelativeOnlyPref =
                Mathf.Clamp(Preferences.Instance.GetFloat("mouseXRelativeSensitivity"), 0.5f, 5f);
            _sensitivityYRelativeOnlyPref =
                Mathf.Clamp(Preferences.Instance.GetFloat("mouseYRelativeSensitivity"), 0.5f, 5f);


            _relativeOnlyOnNoInput = Preferences.Instance.GetBool("mouseRelativeReturnOnlyOnNoInput");
            _relativeRatePref = Mathf.Clamp(Preferences.Instance.GetFloat("mouseRelativeRate"), 1, 100);
            _relativeCurvePref = Mathf.Clamp(Preferences.Instance.GetFloat("mouseRelativeCurve"), 1, 100);

            _deadzonePref = Mathf.Clamp(Preferences.Instance.GetFloat("mouseDeadzone"), 0, 0.5f);
            _powerCurvePref = Mathf.Clamp(Preferences.Instance.GetFloat("mousePowerCurve"), 1, 3);

            inGameUI.MouseWidget.UpdateDeadzone(_deadzonePref);
        }
    }
}