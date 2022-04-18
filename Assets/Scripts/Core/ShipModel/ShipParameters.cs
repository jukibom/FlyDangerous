using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.ShipModel {
    public class ShipParameters {
        public static readonly ShipParameters Defaults = new() {
            mass = 1100f,
            drag = 0f,
            angularDrag = 0f,
            inertiaTensorMultiplier = 175f,
            maxSpeed = 800f,
            maxBoostSpeed = 932f,
            maxThrust = 220000f,
            maxAngularVelocity = 7f,
            torqueThrustMultiplier = 0.08f,
            throttleMultiplier = 1f,
            latHMultiplier = 0.5f,
            latVMultiplier = 0.7f,
            pitchMultiplier = 1f,
            rollMultiplier = 0.3f,
            yawMultiplier = 0.8f,
            thrustBoostMultiplier = 3.25f,
            torqueBoostMultiplier = 2f,
            totalBoostTime = 5f,
            totalBoostRotationalTime = 6f,
            boostMaxSpeedDropOffTime = 12f,
            boostRechargeTime = 4f,
            boostCapacitorPercentCost = 70f,
            boostCapacityPercentChargeRate = 10f,
            minUserLimitedVelocity = 250f
        };

        public float angularDrag;
        public float boostCapacitorPercentCost;
        public float boostCapacityPercentChargeRate;
        public float boostMaxSpeedDropOffTime;
        public float boostRechargeTime;
        public float drag;
        public float inertiaTensorMultiplier;
        public float latHMultiplier;
        public float latVMultiplier;
        public float mass;
        public float maxAngularVelocity;
        public float maxBoostSpeed;
        public float maxSpeed;
        public float maxThrust;
        public float minUserLimitedVelocity;
        public float pitchMultiplier;
        public float rollMultiplier;
        public float throttleMultiplier;
        public float thrustBoostMultiplier;
        public float torqueBoostMultiplier;
        public float torqueThrustMultiplier;
        public float totalBoostRotationalTime;
        public float totalBoostTime;
        public float yawMultiplier;

        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [CanBeNull]
        public static ShipParameters FromJsonString(string json) {
            try {
                return JsonConvert.DeserializeObject<ShipParameters>(json);
            }
            catch (Exception e) {
                Debug.LogWarning(e.Message);
                return null;
            }
        }
    }
}