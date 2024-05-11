using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            boostSpoolUpTime = 1f,
            totalBoostTime = 5f,
            totalBoostRotationalTime = 6f,
            boostMaxSpeedDropOffTime = 12f,
            boostRechargeTime = 4f,
            boostCapacitorPercentCost = 70f,
            boostCapacityPercentChargeRate = 10f,
            boostMaxDivertablePower = 0.4f,
            minUserLimitedVelocity = 250f,
            boostDivertEfficiency = 1f
        };

        private class NullableParameters{
            public float? angularDrag { get; set; }
            public float? boostCapacitorPercentCost { get; set; }
            public float? boostCapacityPercentChargeRate { get; set; }
            public float? boostMaxDivertablePower { get; set; }
            public float? boostMaxSpeedDropOffTime { get; set; }
            public float? boostRechargeTime { get; set; }
            public float? boostSpoolUpTime { get; set; }
            public float? drag { get; set; }
            public float? inertiaTensorMultiplier { get; set; }
            public float? latHMultiplier { get; set; }
            public float? latVMultiplier { get; set; }
            public float? mass { get; set; }
            public float? maxAngularVelocity { get; set; }
            public float? maxBoostSpeed { get; set; }
            public float? maxSpeed { get; set; }
            public float? maxThrust { get; set; }
            public float? minUserLimitedVelocity { get; set; }
            public float? pitchMultiplier { get; set; }
            public float? rollMultiplier { get; set; }
            public float? throttleMultiplier { get; set; }
            public float? thrustBoostMultiplier { get; set; }
            public float? torqueBoostMultiplier { get; set; }
            public float? torqueThrustMultiplier { get; set; }
            public float? totalBoostRotationalTime { get; set; }
            public float? totalBoostTime { get; set; }
            public float? yawMultiplier { get; set; }
            public float? boostDivertEfficiency { get; set; }
        }

        public float angularDrag;
        public float boostCapacitorPercentCost;
        public float boostCapacityPercentChargeRate;
        public float boostMaxDivertablePower;
        public float boostMaxSpeedDropOffTime;
        public float boostRechargeTime;
        public float boostSpoolUpTime;
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
        public float boostDivertEfficiency;

        public string ToJsonString() {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            var jObject = JObject.Parse(json);
            if ((float)jObject.GetValue("mass") == ShipParameters.Defaults.mass)
                jObject.Remove("mass");
            if ((float)jObject.GetValue("inertiaTensorMultiplier") == ShipParameters.Defaults.inertiaTensorMultiplier)
                jObject.Remove("inertiaTensorMultiplier");

            return jObject.ToString(Formatting.Indented);
        }

        [CanBeNull]
        public static ShipParameters FromJsonString(string json) {
            try {
                ShipParameters returnedParameters = ShipParameters.Defaults;

                var parameters = JsonConvert.DeserializeObject<NullableParameters>(json);
                
                var type = typeof(ShipParameters);
                var properties = type.GetProperties();

                foreach (var property in properties)
                {
                    if (property.GetValue(parameters) != null)
                            property.SetValue(returnedParameters, property.GetValue(parameters));
                }
                return returnedParameters;
            }
            catch (Exception e) {
                Debug.LogWarning(e.Message);
                return null;
            }
        }
    }
}