using System.Globalization;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ShipModel {
    public class CalidrisShipModel : SimpleShipModel {
        [SerializeField] private Text velocityIndicatorText;
        [SerializeField] private Image accelerationBar;

        [SerializeField] private Text boostIndicatorText;
        [SerializeField] private Image boostCapacitorBar;

        [SerializeField] private Text boostChargeText;
        [SerializeField] private Image boostReadyIcon;

        [SerializeField] private Image vectorAssistIcon;
        [SerializeField] private Text vectorAssistText;

        [SerializeField] private Image rotationalAssistIcon;
        [SerializeField] private Text rotationAssistText;

        [SerializeField] private Image velocityLimiterIcon;
        [SerializeField] private Text velocityLimiterText;

        [SerializeField] private Image shipLightIcon;
        [SerializeField] private Text gForceNumberText;

        private readonly Color32 _activeColor = new(0, 153, 225, 255);
        private readonly Color32 _disabledColor = new(39, 72, 91, 255);
        private readonly Color32 _notificationColor = new(195, 195, 30, 255);
        private readonly Color32 _positiveColor = new(30, 195, 28, 255);
        private readonly Color32 _warningColor = new(195, 28, 30, 255);

        // Lerping fun
        private float _previousAccelerationBarAmount;
        private float _previousGForce;

        public override void OnEnable() {
            Game.OnRestart += Restart;
            base.OnEnable();
        }

        public override void OnDisable() {
            Game.OnRestart -= Restart;
            base.OnDisable();
        }

        public override void UpdateIndicators(ShipIndicatorData shipIndicatorData) {
            #region Simple Indicators

            vectorAssistIcon.color = shipIndicatorData.vectorFlightAssistActive ? _positiveColor : _warningColor;
            vectorAssistText.text = shipIndicatorData.vectorFlightAssistActive ? "VFA\nON" : "VFA\nOFF";

            rotationalAssistIcon.color = shipIndicatorData.rotationalFlightAssistActive ? _positiveColor : _warningColor;
            rotationAssistText.text = shipIndicatorData.rotationalFlightAssistActive ? "RFA\nON" : "RFA\nOFF";

            velocityLimiterIcon.color = shipIndicatorData.velocityLimiterActive ? _activeColor : _disabledColor;
            velocityLimiterText.text = shipIndicatorData.velocityLimiterActive ? "V-LIM\nON" : "V-LIM\nOFF";

            shipLightIcon.color = shipIndicatorData.lightsActive ? _activeColor : _disabledColor;

            #endregion

            #region Velocity

            velocityIndicatorText.text = shipIndicatorData.velocity.ToString(CultureInfo.InvariantCulture);

            // special use-case for acceleration bar depending on flight assist (switch to throttle input)
            var accelerationBarAmount = shipIndicatorData.vectorFlightAssistActive
                ? shipIndicatorData.throttlePosition
                : shipIndicatorData.acceleration;

            accelerationBarAmount = Mathf.Lerp(_previousAccelerationBarAmount, accelerationBarAmount, 0.1f);

            _previousAccelerationBarAmount = Mathf.Clamp(accelerationBarAmount, -1, 1);

            // if reverse, switch to a yellow colour and invert
            accelerationBar.transform.localRotation = Quaternion.Euler(0, 0, 45);
            accelerationBar.fillClockwise = true;
            var accelerationBarBaseActiveColor = _activeColor;
            if (shipIndicatorData.vectorFlightAssistActive && accelerationBarAmount < 0) {
                accelerationBar.color = _notificationColor;
                accelerationBarBaseActiveColor = _notificationColor;
                accelerationBar.transform.localRotation = Quaternion.Euler(0, 0, 135);
                accelerationBar.fillClockwise = false;
            }

            // animate bar
            var accelerationDrawFillAmount = Mathf.Abs(accelerationBarAmount);
            accelerationBar.fillAmount = MathfExtensions.Remap(0, 1, 0, 0.755f, accelerationDrawFillAmount);

            // fade to red near end of bar 
            if (accelerationDrawFillAmount > 0.7f)
                accelerationBar.color = Color.Lerp(accelerationBarBaseActiveColor, _warningColor,
                    MathfExtensions.Remap(0.95f, 1, 0, 1, accelerationDrawFillAmount));
            else
                accelerationBar.color = accelerationBarBaseActiveColor;

            #endregion

            #region Boost

            boostIndicatorText.text = ((int)shipIndicatorData.boostCapacitorPercent).ToString(CultureInfo.InvariantCulture) + "%";
            boostCapacitorBar.fillAmount = Mathf.Lerp(
                boostCapacitorBar.fillAmount,
                MathfExtensions.Remap(0, 100, 0, 0.755f, shipIndicatorData.boostCapacitorPercent),
                0.1f
            );

            if (shipIndicatorData.boostCapacitorPercent > 80)
                boostCapacitorBar.color = Color.Lerp(_activeColor, _positiveColor,
                    MathfExtensions.Remap(
                        80,
                        90,
                        0, 1, shipIndicatorData.boostCapacitorPercent
                    )
                );
            else if (shipIndicatorData.boostCapacitorPercent < 30f)
                boostCapacitorBar.color = Color.Lerp(_activeColor, _warningColor,
                    MathfExtensions.Remap(30, 15, 0, 1, shipIndicatorData.boostCapacitorPercent));
            else
                boostCapacitorBar.color = _activeColor;

            var boostWarningColor = shipIndicatorData.boostTimerReady ? _notificationColor : _warningColor;
            boostReadyIcon.color = shipIndicatorData.boostTimerReady && shipIndicatorData.boostChargeReady ? _positiveColor : boostWarningColor;
            boostChargeText.text = shipIndicatorData.boostTimerReady && shipIndicatorData.boostChargeReady
                ? "BOOST READY"
                : !shipIndicatorData.boostTimerReady
                    ? "BOOSTING"
                    : "BOOST CHARGING";

            #endregion

            #region GForce

            var gForce = Mathf.Lerp(_previousGForce, shipIndicatorData.gForce, 0.05f);
            _previousGForce = gForce;
            gForceNumberText.text = $"{gForce:0.0}";

            #endregion
        }

        private void Restart() {
            _previousGForce = 0;
        }
    }
}