using System;
using System.Runtime.CompilerServices;
using Bhaptics.Tact;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Animations;



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

        private static string[] nameList = {
            "angularDrag",
            "boostCapacitorPercentCost",
            "boostCapacityPercentChargeRate",
            "boostMaxDivertablePower",
            "boostMaxSpeedDropOffTime",
            "boostRechargeTime",
            "boostSpoolUpTime",
            "drag",
            "inertiaTensorMultiplier",
            "latHMultiplier",
            "latVMultiplier",
            "mass",
            "maxAngularVelocity",
            "maxBoostSpeed",
            "maxSpeed",
            "maxThrust",
            "minUserLimitedVelocity",
            "pitchMultiplier",
            "rollMultiplier",
            "throttleMultiplier",
            "thrustBoostMultiplier",
            "torqueBoostMultiplier",
            "torqueThrustMultiplier",
            "totalBoostRotationalTime",
            "totalBoostTime",
            "yawMultiplier",
            "boostDivertEfficiency"
        };

        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [CanBeNull]
        public static ShipParameters FromJsonString(string json) {
            
            try {
                JObject parameters = JObject.Parse(json);
                JObject defaults = JObject.Parse(ShipParameters.Defaults.ToJsonString());
                
                foreach (string parameter in nameList)
                {
                    if (!parameters.ContainsKey(parameter))
                            parameters.Add(parameter, defaults[parameter]);
                }
                return JsonConvert.DeserializeObject<ShipParameters>(parameters.ToString());
            }
            catch (Exception e) {
                Debug.LogWarning(e.Message);
                return null;
            }
        }

    }
}