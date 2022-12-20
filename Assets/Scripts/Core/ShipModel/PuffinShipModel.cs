﻿using System.Globalization;
using Core.ShipModel.Feedback.interfaces;
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
            base.OnShipInstrumentUpdate(shipInstrumentData);

            if (velocityIndicator != null) velocityIndicator.text = shipInstrumentData.Speed.ToString(CultureInfo.InvariantCulture);

            if (accelerationBar != null) {
                accelerationBar.fillAmount = shipInstrumentData.AccelerationMagnitudeNormalised.Remap(0, 1, 0, 0.755f);
                accelerationBar.color = Color.Lerp(Color.green, Color.red, shipInstrumentData.AccelerationMagnitudeNormalised);
            }

            if (boostIndicator != null) boostIndicator.text = ((int)shipInstrumentData.BoostCapacitorPercent).ToString(CultureInfo.InvariantCulture) + "%";

            if (boostCapacitorBar != null) {
                boostCapacitorBar.fillAmount = shipInstrumentData.BoostCapacitorPercent.Remap(0, 100, 0, 0.775f);
                boostCapacitorBar.color = Color.Lerp(Color.red, Color.green, shipInstrumentData.BoostCapacitorPercent / 100);
            }
        }
    }
}