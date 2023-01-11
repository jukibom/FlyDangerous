using System.Globalization;
using Core.ShipModel.Feedback.interfaces;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI {
    [RequireComponent(typeof(CanvasGroup))]
    public class ShipStats : MonoBehaviour, IShipInstruments {
        // indicator UI
        // TODO: move this somewhere 
        [SerializeField] private Text velocityIndicatorText;
        [SerializeField] private Image accelerationBar;

        [SerializeField] private Text boostIndicatorText;
        [SerializeField] private Image boostCapacitorBar;

        [SerializeField] private Text boostChargeText;
        [SerializeField] private Image boostReadyIcon;

        private readonly Color32 _activeColor = new(0, 153, 225, 255);
        private readonly Color32 _disabledColor = new(39, 72, 91, 255);
        private readonly Color32 _notificationColor = new(195, 195, 30, 255);
        private readonly Color32 _positiveColor = new(30, 195, 28, 255);
        private readonly Color32 _warningColor = new(195, 28, 30, 255);
        private CanvasGroup _canvasGroup;

        // Lerping fun
        private float _previousAccelerationBarAmount;
        private float _previousGForce;
        private float _targetUIAlpha;

        public bool ForceHidden { get; set; }

        private void FixedUpdate() {
            _canvasGroup.alpha = Mathf.Clamp(_canvasGroup.alpha + _targetUIAlpha * Time.fixedDeltaTime * 2, 0, 1);

            if (ForceHidden) _canvasGroup.alpha = 0;
        }

        private void OnEnable() {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            ForceHidden = true;
        }

        public void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
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
                boostCapacitorBar.color = Color.Lerp(_activeColor, _positiveColor,
                    shipInstrumentData.BoostCapacitorPercent.Remap(80, 90, 0, 1));
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

            #endregion }
        }

        public void SetStatsVisible(bool visible) {
            _targetUIAlpha = visible ? 1 : -1;
        }
    }
}