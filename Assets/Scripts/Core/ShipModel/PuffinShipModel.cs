using System.Globalization;
using Core.ShipModel.ShipIndicator;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ShipModel {
    public class PuffinShipModel : SimpleShipModel {
        [SerializeField] private Text velocityIndicator;
        [SerializeField] private Image accelerationBar;
        [SerializeField] private Text boostIndicator;
        [SerializeField] private Image boostCapacitorBar;

        public override void OnShipIndicatorUpdate(IShipIndicatorData shipIndicatorData) {
            if (velocityIndicator != null) velocityIndicator.text = shipIndicatorData.VelocityMagnitude.ToString(CultureInfo.InvariantCulture);

            if (accelerationBar != null) {
                accelerationBar.fillAmount = MathfExtensions.Remap(0, 1, 0, 0.755f, shipIndicatorData.AccelerationMagnitudeNormalised);
                accelerationBar.color = Color.Lerp(Color.green, Color.red, shipIndicatorData.AccelerationMagnitudeNormalised);
            }

            if (boostIndicator != null) boostIndicator.text = ((int)shipIndicatorData.BoostCapacitorPercent).ToString(CultureInfo.InvariantCulture) + "%";

            if (boostCapacitorBar != null) {
                boostCapacitorBar.fillAmount = MathfExtensions.Remap(0, 100, 0, 0.775f, shipIndicatorData.BoostCapacitorPercent);
                boostCapacitorBar.color = Color.Lerp(Color.red, Color.green, shipIndicatorData.BoostCapacitorPercent / 100);
            }
        }
    }
}