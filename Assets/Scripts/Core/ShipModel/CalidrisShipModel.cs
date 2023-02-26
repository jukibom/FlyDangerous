using System.Globalization;
using Core.ShipModel.Feedback.interfaces;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ShipModel {
    public class CalidrisShipModel : SimpleShipModel {
        [SerializeField] private AudioSource proximityWarningAudioSource;

        [SerializeField] private GameObject overchargeWarningLight;
        [SerializeField] private GameObject overchargeWarning;
        [SerializeField] private Text overchargeAmountText;

        [SerializeField] private Light proximityWarningLight;
        [SerializeField] private GameObject proximityWarning;

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
        private float _flashingLightTimer;

        private float _targetProximityWarning;

        public override void OnEnable() {
            Game.OnRestart += Restart;
            base.OnEnable();
        }

        public override void OnDisable() {
            Game.OnRestart -= Restart;
            base.OnDisable();
        }

        public override void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
            base.OnShipInstrumentUpdate(shipInstrumentData);

            #region Simple Indicators

            vectorAssistIcon.color = shipInstrumentData.VectorFlightAssistActive ? _positiveColor : _warningColor;
            vectorAssistText.text = shipInstrumentData.VectorFlightAssistActive ? "VFA\nON" : "VFA\nOFF";

            rotationalAssistIcon.color = shipInstrumentData.RotationalFlightAssistActive ? _positiveColor : _warningColor;
            rotationAssistText.text = shipInstrumentData.RotationalFlightAssistActive ? "RFA\nON" : "RFA\nOFF";

            velocityLimiterIcon.color = shipInstrumentData.VelocityLimiterActive ? _activeColor : _disabledColor;
            velocityLimiterText.text = shipInstrumentData.VelocityLimiterActive ? "V-LIM\nON" : "V-LIM\nOFF";

            shipLightIcon.color = shipInstrumentData.LightsActive ? _activeColor : _disabledColor;

            #endregion

            #region Velocity

            velocityIndicatorText.text = shipInstrumentData.Speed.ToString(CultureInfo.InvariantCulture);

            // special use-case for acceleration bar depending on flight assist (switch to throttle input)
            var accelerationBarAmount = shipInstrumentData.VectorFlightAssistActive
                ? shipInstrumentData.ThrottlePositionNormalised
                : shipInstrumentData.AccelerationMagnitudeNormalised;

            accelerationBarAmount = Mathf.Lerp(_previousAccelerationBarAmount, accelerationBarAmount, 0.1f);

            _previousAccelerationBarAmount = Mathf.Clamp(accelerationBarAmount, -1, 1);

            // if reverse, switch to a yellow colour and invert
            accelerationBar.transform.localRotation = Quaternion.Euler(0, 0, 45);
            accelerationBar.fillClockwise = true;
            var accelerationBarBaseActiveColor = _activeColor;
            if (shipInstrumentData.VectorFlightAssistActive && accelerationBarAmount < 0) {
                accelerationBar.color = _notificationColor;
                accelerationBarBaseActiveColor = _notificationColor;
                accelerationBar.transform.localRotation = Quaternion.Euler(0, 0, 135);
                accelerationBar.fillClockwise = false;
            }

            // animate bar
            var accelerationDrawFillAmount = Mathf.Abs(accelerationBarAmount);
            accelerationBar.fillAmount = accelerationDrawFillAmount.Remap(0, 1, 0, 0.755f);

            // fade to red near end of bar 
            if (accelerationDrawFillAmount > 0.7f)
                accelerationBar.color = Color.Lerp(accelerationBarBaseActiveColor, _warningColor,
                    accelerationDrawFillAmount.Remap(0.95f, 1, 0, 1));
            else
                accelerationBar.color = accelerationBarBaseActiveColor;

            #endregion

            #region Boost

            boostIndicatorText.text = ((int)shipInstrumentData.BoostCapacitorPercent).ToString(CultureInfo.InvariantCulture) + "%";
            boostCapacitorBar.fillAmount = Mathf.Lerp(
                boostCapacitorBar.fillAmount,
                shipInstrumentData.BoostCapacitorPercent.Remap(0, 100, 0, 0.755f),
                0.1f
            );

            if (shipInstrumentData.BoostCapacitorPercent > 80)
                boostCapacitorBar.color = Color.Lerp(_activeColor, _positiveColor, shipInstrumentData.BoostCapacitorPercent.Remap(80, 90, 0, 1));
            else if (shipInstrumentData.BoostCapacitorPercent < 30f)
                boostCapacitorBar.color = Color.Lerp(_activeColor, _warningColor,
                    shipInstrumentData.BoostCapacitorPercent.Remap(30, 15, 0, 1));
            else
                boostCapacitorBar.color = _activeColor;

            var boostWarningColor = shipInstrumentData.BoostTimerReady ? _notificationColor : _warningColor;
            boostReadyIcon.color = shipInstrumentData.BoostTimerReady && shipInstrumentData.BoostChargeReady ? _positiveColor : boostWarningColor;
            boostChargeText.text = shipInstrumentData.BoostTimerReady && shipInstrumentData.BoostChargeReady
                ? "BOOST READY"
                : !shipInstrumentData.BoostTimerReady
                    ? "BOOSTING"
                    : "BOOST CHARGING";

            #endregion

            #region GForce

            var gForce = Mathf.Lerp(_previousGForce, shipInstrumentData.GForce, 0.05f);
            _previousGForce = gForce;
            gForceNumberText.text = $"{gForce:0.0}";

            #endregion

            #region Overcharge Alert

            overchargeWarning.SetActive(false);
            overchargeWarningLight.SetActive(false);
            if (shipInstrumentData.ThrustOverchargeNormalized > 0.15f) {
                overchargeWarning.SetActive(true);
                overchargeWarningLight.SetActive(Mathf.Cos(_flashingLightTimer * 20 / Mathf.PI) > 0);
            }

            // show overcharge text to nearest 10%
            overchargeAmountText.text = $"+{Mathf.Round(shipInstrumentData.ThrustOverchargeNormalized * 10) * 10}%";

            #endregion

            #region Proximity Alert

            float proximity = 0;
            var showProximityWarning = false;
            if (shipInstrumentData.ProximityWarning) {
                proximity = 1 - shipInstrumentData.ProximityWarningSeconds / 5;
                showProximityWarning = Mathf.Cos(_flashingLightTimer * 20 / Mathf.PI) > 0;
            }

            proximityWarning.SetActive(showProximityWarning);

            var lerpAmount = proximity > _targetProximityWarning ? 0.02f : 0.08f;
            _targetProximityWarning = Mathf.Lerp(_targetProximityWarning, proximity, lerpAmount);

            proximityWarningAudioSource.volume = _targetProximityWarning * 0.05f;
            proximityWarningLight.intensity = _targetProximityWarning * 0.2f;

            #endregion

            _flashingLightTimer += Time.fixedDeltaTime;
        }

        private void Restart() {
            _previousGForce = 0;
        }
    }
}