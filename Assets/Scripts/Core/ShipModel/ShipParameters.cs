using System;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Core.ShipModel
{
    public class ShipParameters
    {

        private class DefaultValues
        {
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
        }

        public static ShipParameters Defaults()
        {
            return new(
                
                );
        }

        [JsonProperty(Order = 1)]
        [DefaultValue(DefaultValues.boostCapacitorPercentCost)]
        public float boostCapacitorPercentCost = DefaultValues.boostCapacitorPercentCost;
        [JsonProperty(Order = 2)]
        [DefaultValue(DefaultValues.boostCapacityPercentChargeRate)]
        public float boostCapacityPercentChargeRate = DefaultValues.boostCapacityPercentChargeRate;
        [JsonProperty(Order = 3)]
        [DefaultValue(DefaultValues.boostMaxDivertablePower)]
        public float boostMaxDivertablePower = DefaultValues.boostMaxDivertablePower;
        [JsonProperty(Order = 4)]
        [DefaultValue(DefaultValues.boostMaxSpeedDropOffTime)]
        public float boostMaxSpeedDropOffTime = DefaultValues.boostMaxSpeedDropOffTime;
        [JsonProperty(Order = 5)]
        [DefaultValue(DefaultValues.boostRechargeTime)]
        public float boostRechargeTime = DefaultValues.boostRechargeTime;
        [JsonProperty(Order = 6)]
        [DefaultValue(DefaultValues.boostSpoolUpTime)]
        public float boostSpoolUpTime = DefaultValues.boostSpoolUpTime;
        [JsonProperty(Order = 7)]
        [DefaultValue(DefaultValues.drag)]
        public float drag = DefaultValues.drag;
        [JsonProperty(Order = 8)]
        [DefaultValue(DefaultValues.angularDrag)]
        public float angularDrag = DefaultValues.angularDrag;
        [JsonProperty(Order = 9)]
        [DefaultValue(DefaultValues.inertiaTensorMultiplier)]
        public float inertiaTensorMultiplier = DefaultValues.inertiaTensorMultiplier;
        [JsonProperty(Order = 10)]
        [DefaultValue(DefaultValues.latHMultiplier)]
        public float latHMultiplier = DefaultValues.latHMultiplier;
        [JsonProperty(Order = 11)]
        [DefaultValue(DefaultValues.latVMultiplier)]
        public float latVMultiplier = DefaultValues.latVMultiplier;
        [JsonProperty(Order = 12)]
        [DefaultValue(DefaultValues.mass)]
        public float mass = DefaultValues.mass;
        [JsonProperty(Order = 13)]
        [DefaultValue(DefaultValues.maxAngularVelocity)]
        public float maxAngularVelocity = DefaultValues.maxAngularVelocity;
        [JsonProperty(Order = 14)]
        [DefaultValue(DefaultValues.maxBoostSpeed)]
        public float maxBoostSpeed = DefaultValues.maxBoostSpeed;
        [JsonProperty(Order = 15)]
        [DefaultValue(DefaultValues.maxSpeed)]
        public float maxSpeed = DefaultValues.maxSpeed;
        [JsonProperty(Order = 16)]
        [DefaultValue(DefaultValues.maxThrust)]
        public float maxThrust = DefaultValues.maxThrust;
        [JsonProperty(Order = 17)]
        [DefaultValue(DefaultValues.minUserLimitedVelocity)]
        public float minUserLimitedVelocity = DefaultValues.minUserLimitedVelocity;
        [JsonProperty(Order = 18)]
        [DefaultValue(DefaultValues.pitchMultiplier)]
        public float pitchMultiplier = DefaultValues.pitchMultiplier;
        [JsonProperty(Order = 19)]
        [DefaultValue(DefaultValues.yawMultiplier)]
        public float yawMultiplier = DefaultValues.yawMultiplier;
        [JsonProperty(Order = 20)]
        [DefaultValue(DefaultValues.rollMultiplier)]
        public float rollMultiplier = DefaultValues.rollMultiplier;
        [JsonProperty(Order = 21)]
        [DefaultValue(DefaultValues.throttleMultiplier)]
        public float throttleMultiplier = DefaultValues.throttleMultiplier;
        [JsonProperty(Order = 22)]
        [DefaultValue(DefaultValues.thrustBoostMultiplier)]
        public float thrustBoostMultiplier = DefaultValues.thrustBoostMultiplier;
        [JsonProperty(Order = 23)]
        [DefaultValue(DefaultValues.torqueBoostMultiplier)]
        public float torqueBoostMultiplier = DefaultValues.torqueBoostMultiplier;
        [JsonProperty(Order = 24)]
        [DefaultValue(DefaultValues.torqueThrustMultiplier)]
        public float torqueThrustMultiplier = DefaultValues.torqueThrustMultiplier;
        [JsonProperty(Order = 25)]
        [DefaultValue(DefaultValues.totalBoostRotationalTime)]
        public float totalBoostRotationalTime = DefaultValues.totalBoostRotationalTime;
        [JsonProperty(Order = 26)]
        [DefaultValue(DefaultValues.totalBoostTime)]
        public float totalBoostTime = DefaultValues.totalBoostTime;

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [CanBeNull]
        public static ShipParameters FromJsonString(string json)
        {

            try
            {
                JObject parameters = JObject.Parse(json);
                JObject defaults = JObject.Parse(ShipParameters.Defaults().ToJsonString());

                foreach (string parameter in defaults.Properties())
                    if (!parameters.ContainsKey(parameter))
                        parameters.Add(parameter, defaults[parameter]);
                
                return JsonConvert.DeserializeObject<ShipParameters>(parameters.ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                return null;
            }
        }
    }
}