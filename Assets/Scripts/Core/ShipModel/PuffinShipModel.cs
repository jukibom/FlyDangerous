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

        public override void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData) {
            if (velocityIndicator != null) velocityIndicator.text = shipInstrumentData.VelocityMagnitude.ToString(CultureInfo.InvariantCulture);

            if (accelerationBar != null) {
                accelerationBar.fillAmount = MathfExtensions.Remap(0, 1, 0, 0.755f, shipInstrumentData.AccelerationMagnitudeNormalised);
                accelerationBar.color = Color.Lerp(Color.green, Color.red, shipInstrumentData.AccelerationMagnitudeNormalised);
            }

            if (boostIndicator != null) boostIndicator.text = ((int)shipInstrumentData.BoostCapacitorPercent).ToString(CultureInfo.InvariantCulture) + "%";

            if (boostCapacitorBar != null) {
                boostCapacitorBar.fillAmount = MathfExtensions.Remap(0, 100, 0, 0.775f, shipInstrumentData.BoostCapacitorPercent);
                boostCapacitorBar.color = Color.Lerp(Color.red, Color.green, shipInstrumentData.BoostCapacitorPercent / 100);
            }
        }
    }
}