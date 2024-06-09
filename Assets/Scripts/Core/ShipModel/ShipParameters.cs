using System;
using System.ComponentModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;



namespace Core.ShipModel {
    public class ShipParameters {
        private class DefaultValues{
            public const float mass = 1100f;
            public const float drag = 0f;
            public const float angularDrag = 0f;
            public const float inertiaTensorMultiplier = 175f;
            public const float maxSpeed = 800f;
            public const float maxBoostSpeed = 932f;
            public const float maxThrust = 220000f;
            public const float maxAngularVelocity = 7f;
            public const float torqueThrustMultiplier = 0.08f;
            public const float throttleMultiplier = 1f;
            public const float latHMultiplier = 0.5f;
            public const float latVMultiplier = 0.7f;
            public const float pitchMultiplier = 1f;
            public const float rollMultiplier = 0.3f;
            public const float yawMultiplier = 0.8f;
            public const float thrustBoostMultiplier = 3.25f;
            public const float torqueBoostMultiplier = 2f;
            public const float boostSpoolUpTime = 1f;
            public const float totalBoostTime = 5f;
            public const float totalBoostRotationalTime = 6f;
            public const float boostMaxSpeedDropOffTime = 12f;
            public const float boostRechargeTime = 4f;
            public const float boostCapacitorPercentCost = 70f;
            public const float boostCapacityPercentChargeRate = 10f;
            public const float boostMaxDivertablePower = 0.4f;
            public const float minUserLimitedVelocity = 250f;
            public const float boostDivertEfficiency = 1f;
        }

        private static string[] nameList = { 
            //IMPORTANT changing the order of these will change the hash string of the shipParameters. Only add parameters at the end! 
            "boostCapacitorPercentCost",
            "boostCapacityPercentChargeRate",
            "boostMaxDivertablePower",
            "boostDivertEfficiency",
            "boostMaxSpeedDropOffTime",
            "boostRechargeTime",
            "boostSpoolUpTime",
            "drag",
            "angularDrag",
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
            "yawMultiplier",
            "rollMultiplier",
            "throttleMultiplier",
            "thrustBoostMultiplier",
            "torqueBoostMultiplier",
            "torqueThrustMultiplier",
            "totalBoostRotationalTime",
            "totalBoostTime"
        };

        public static  ShipParameters CreateDefaults() {
            return new();
        }
        
        [DefaultValue(DefaultValues.boostCapacitorPercentCost)]
        public float boostCapacitorPercentCost = DefaultValues.boostCapacitorPercentCost;
        [DefaultValue(DefaultValues.boostCapacityPercentChargeRate)]
        public float boostCapacityPercentChargeRate = DefaultValues.boostCapacityPercentChargeRate;
        [DefaultValue(DefaultValues.boostMaxDivertablePower)]
        public float boostMaxDivertablePower = DefaultValues.boostMaxDivertablePower;
        [DefaultValue(DefaultValues.boostDivertEfficiency)]
        public float boostDivertEfficiency = DefaultValues.boostDivertEfficiency;
        [DefaultValue(DefaultValues.boostMaxSpeedDropOffTime)]
        public float boostMaxSpeedDropOffTime = DefaultValues.boostMaxSpeedDropOffTime;
        [DefaultValue(DefaultValues.boostRechargeTime)]
        public float boostRechargeTime = DefaultValues.boostRechargeTime;
        [DefaultValue(DefaultValues.boostSpoolUpTime)]
        public float boostSpoolUpTime = DefaultValues.boostSpoolUpTime;
        [DefaultValue(DefaultValues.drag)]
        public float drag = DefaultValues.drag;
        [DefaultValue(DefaultValues.angularDrag)]
        public float angularDrag = DefaultValues.angularDrag;
        [DefaultValue(DefaultValues.inertiaTensorMultiplier)]
        public float inertiaTensorMultiplier = DefaultValues.inertiaTensorMultiplier;
        [DefaultValue(DefaultValues.latHMultiplier)]
        public float latHMultiplier = DefaultValues.latHMultiplier;
        [DefaultValue(DefaultValues.latVMultiplier)]
        public float latVMultiplier = DefaultValues.latVMultiplier;
        [DefaultValue(DefaultValues.mass)]
        public float mass = DefaultValues.mass;
        [DefaultValue(DefaultValues.maxAngularVelocity)]
        public float maxAngularVelocity = DefaultValues.maxAngularVelocity;
        [DefaultValue(DefaultValues.maxBoostSpeed)]
        public float maxBoostSpeed = DefaultValues.maxBoostSpeed;
        [DefaultValue(DefaultValues.maxSpeed)]
        public float maxSpeed = DefaultValues.maxSpeed;
        [DefaultValue(DefaultValues.maxThrust)]
        public float maxThrust = DefaultValues.maxThrust;
        [DefaultValue(DefaultValues.minUserLimitedVelocity)]
        public float minUserLimitedVelocity = DefaultValues.minUserLimitedVelocity;
        [DefaultValue(DefaultValues.pitchMultiplier)]
        public float pitchMultiplier = DefaultValues.pitchMultiplier;
        [DefaultValue(DefaultValues.yawMultiplier)]
        public float yawMultiplier = DefaultValues.yawMultiplier;
        [DefaultValue(DefaultValues.rollMultiplier)]
        public float rollMultiplier = DefaultValues.rollMultiplier;
        [DefaultValue(DefaultValues.throttleMultiplier)]
        public float throttleMultiplier = DefaultValues.throttleMultiplier;
        [DefaultValue(DefaultValues.thrustBoostMultiplier)]
        public float thrustBoostMultiplier = DefaultValues.thrustBoostMultiplier;
        [DefaultValue(DefaultValues.torqueBoostMultiplier)]
        public float torqueBoostMultiplier = DefaultValues.torqueBoostMultiplier;
        [DefaultValue(DefaultValues.torqueThrustMultiplier)]
        public float torqueThrustMultiplier = DefaultValues.torqueThrustMultiplier;
        [DefaultValue(DefaultValues.totalBoostRotationalTime)]
        public float totalBoostRotationalTime = DefaultValues.totalBoostRotationalTime;
        [DefaultValue(DefaultValues.totalBoostTime)]
        public float totalBoostTime = DefaultValues.totalBoostTime;

        

        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [CanBeNull]
        public static ShipParameters FromJsonString(string json) {
            
            try {
                JObject parameters = JObject.Parse(json);
                JObject defaults = JObject.Parse(ShipParameters.CreateDefaults().ToJsonString());
                
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

        public static string ToHashString(ShipParameters shipParameters)
        {
            string hashString = "";

            JObject parameters = JObject.Parse(shipParameters.ToJsonString());
            JObject defaults = JObject.Parse(ShipParameters.CreateDefaults().ToJsonString());

            foreach (string parameter in nameList)
            {
                if (parameters[parameter].ToString() != defaults[parameter].ToString())
                    hashString = hashString + parameter + parameters[parameter].ToString();
            }

            return hashString;
        }
    }
}