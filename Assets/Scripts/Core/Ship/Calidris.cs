using System.Globalization;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Ship {
    public class Calidris : SimpleShip {

        private Color32 activeColor = new Color32(0, 153, 225, 255);
        private Color32 disabledColor = new Color32(39, 72, 91, 255);
        private Color32 positiveColor = new Color32(30, 195, 28, 255);
        private Color32 warningColor = new Color32(195, 28, 30, 255);
        private Color32 notificationColor = new Color32(195, 195, 30, 255);

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

        private float _previousGForce;

        public override void UpdateIndicators(ShipIndicatorData shipIndicatorData) {
            #region Simple Indicators
            
            vectorAssistIcon.color = shipIndicatorData.vectorFlightAssistActive ? positiveColor : warningColor;
            vectorAssistText.text = shipIndicatorData.vectorFlightAssistActive ? "VFA\nON" : "VFA\nOFF";
            
            rotationalAssistIcon.color = shipIndicatorData.rotationalFlightAssistActive ? positiveColor : warningColor;
            rotationAssistText.text = shipIndicatorData.rotationalFlightAssistActive ? "RFA\nON" : "RFA\nOFF";

            velocityLimiterIcon.color = shipIndicatorData.velocityLimiterActive ? activeColor : disabledColor;
            velocityLimiterText.text = shipIndicatorData.velocityLimiterActive ? "V-LIM\nON" : "V-LIM\nOFF";

            shipLightIcon.color = shipIndicatorData.lightsActive ? activeColor : disabledColor;

            #endregion
            
            #region Velocity
            velocityIndicatorText.text = shipIndicatorData.velocity.ToString(CultureInfo.InvariantCulture);

            // special use-case for acceleration bar depending on flight assist (switch to throttle input)
            var accelerationBarAmount = shipIndicatorData.vectorFlightAssistActive
                ? Mathf.Abs(shipIndicatorData.throttlePosition)
                : shipIndicatorData.acceleration;
            
            // if reverse, switch to a yellow colour
            var accelerationBarBaseActiveColor = activeColor;
            if (shipIndicatorData.vectorFlightAssistActive && shipIndicatorData.throttlePosition < 0) {
                accelerationBar.color = notificationColor;
                accelerationBarBaseActiveColor = notificationColor;
            }

            // animate bar
            accelerationBar.fillAmount = Mathf.Lerp(accelerationBar.fillAmount, MathfExtensions.Remap(0, 1, 0, 0.755f, accelerationBarAmount), 0.1f);
            
            // fade to red near end of bar 
            if (accelerationBar.fillAmount > 0.7f) {
                accelerationBar.color = Color.Lerp(accelerationBarBaseActiveColor, warningColor, MathfExtensions.Remap(0.95f, 1, 0, 1, accelerationBarAmount));
            }
            else {
                accelerationBar.color = accelerationBarBaseActiveColor;
            }
            #endregion

            #region Boost

            boostIndicatorText.text = ((int) shipIndicatorData.boostCapacitorPercent).ToString(CultureInfo.InvariantCulture) + "%";
            boostCapacitorBar.fillAmount = Mathf.Lerp(
                boostCapacitorBar.fillAmount, 
                MathfExtensions.Remap(0, 100, 0, 0.755f, shipIndicatorData.boostCapacitorPercent), 
                0.1f
            );

            if (shipIndicatorData.boostCapacitorPercent > 95f) {
                boostCapacitorBar.color = Color.Lerp(activeColor, positiveColor, 
                    MathfExtensions.Remap(
                        0.95f / boostCapacitorBar.fillAmount, 
                        boostCapacitorBar.fillAmount,
                        0, 1, boostCapacitorBar.fillAmount
                    )
                );
            }
            else if (shipIndicatorData.boostCapacitorPercent < 20f) {
                boostCapacitorBar.color = Color.Lerp(activeColor, warningColor,
                    MathfExtensions.Remap(0.2f / boostCapacitorBar.fillAmount, 0, 0, 1, boostCapacitorBar.fillAmount));
            }
            else {
                boostCapacitorBar.color = activeColor;
            }
            
            boostReadyIcon.color = shipIndicatorData.boostReady ? positiveColor : warningColor;
            boostChargeText.text = shipIndicatorData.boostReady ? "BOOST READY" : "BOOST CHARGING";

            #endregion
            
            #region GForce
            var gForce = Mathf.Lerp(_previousGForce, shipIndicatorData.gForce, 0.1f);
            _previousGForce = gForce;
            gForceNumberText.text = System.Math.Round(gForce, 1).ToString(CultureInfo.InvariantCulture);
            #endregion
        }
    }
}