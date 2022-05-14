using System.Globalization;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ShipModel {
    public class PuffinShipModel : SimpleShipModel {
        [SerializeField] private Text velocityIndicator;
        [SerializeField] private Image accelerationBar;
        [SerializeField] private Text boostIndicator;
        [SerializeField] private Image boostCapacitorBar;

        public override void UpdateIndicators(ShipIndicatorData shipIndicatorData) {
            if (velocityIndicator != null) velocityIndicator.text = shipIndicatorData.velocity.ToString(CultureInfo.InvariantCulture);

            if (accelerationBar != null) {
                accelerationBar.fillAmount = MathfExtensions.Remap(0, 1, 0, 0.755f, shipIndicatorData.acceleration);
                accelerationBar.color = Color.Lerp(Color.green, Color.red, shipIndicatorData.acceleration);
            }

            if (boostIndicator != null) boostIndicator.text = ((int)shipIndicatorData.boostCapacitorPercent).ToString(CultureInfo.InvariantCulture) + "%";

            if (boostCapacitorBar != null) {
                boostCapacitorBar.fillAmount = MathfExtensions.Remap(0, 100, 0, 0.775f, shipIndicatorData.boostCapacitorPercent);
                boostCapacitorBar.color = Color.Lerp(Color.red, Color.green, shipIndicatorData.boostCapacitorPercent / 100);
            }
        }
    }
}