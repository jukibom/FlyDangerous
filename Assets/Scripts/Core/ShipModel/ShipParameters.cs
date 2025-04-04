using System;
using System.ComponentModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Core.ShipModel
{
    public class ShipParameters
    {
        // Helper for comparison. 
        public static ShipParameters Defaults => new();
        
        // used for member vars and json default declarations
        private struct DefaultValues
        {
            public const float Mass = 1100f;
            public const float Drag = 0f;
            public const float AngularDrag = 0f;
            public const float InertiaTensorMultiplier = 175f;
            public const float MaxSpeed = 800f;
            public const float MaxBoostSpeed = 932f;
            public const float MaxThrust = 220000f;
            public const float MaxAngularVelocity = 7f;
            public const float TorqueThrustMultiplier = 0.08f;
            public const float ThrottleMultiplier = 1f;
            public const float LatHMultiplier = 0.5f;
            public const float LatVMultiplier = 0.7f;
            public const float PitchMultiplier = 1f;
            public const float RollMultiplier = 0.3f;
            public const float YawMultiplier = 0.8f;
            public const float ThrustBoostMultiplier = 3.25f;
            public const float TorqueBoostMultiplier = 2f;
            public const float BoostSpoolUpTime = 1f;
            public const float TotalBoostTime = 5f;
            public const float TotalBoostRotationalTime = 6f;
            public const float BoostMaxSpeedDropOffTime = 12f;
            public const float BoostRechargeTime = 4f;
            public const float BoostCapacitorPercentCost = 70f;
            public const float BoostCapacityPercentChargeRate = 10f;
            public const float BoostMaxDivertablePower = 0.4f;
            public const float MinUserLimitedVelocity = 250f;
            public const bool UseAltBoosters = false;
            public const float BoosterForceMultiplier = 1f;
            public const float BoosterVelocityMultiplier = 1f;
            public const float BoosterThrustMultiplier = 1f;
        }
        
        [JsonProperty(Order = 1), DefaultValue(DefaultValues.Mass)]
        public float mass = DefaultValues.Mass;
        
        [JsonProperty(Order = 2), DefaultValue(DefaultValues.Drag)]
        public float drag = DefaultValues.Drag;
        
        [JsonProperty(Order = 3), DefaultValue(DefaultValues.AngularDrag)]
        public float angularDrag = DefaultValues.AngularDrag;
        
        [JsonProperty(Order = 4), DefaultValue(DefaultValues.InertiaTensorMultiplier)]
        public float inertiaTensorMultiplier = DefaultValues.InertiaTensorMultiplier;
        
        [JsonProperty(Order = 5), DefaultValue(DefaultValues.MaxSpeed)]
        public float maxSpeed = DefaultValues.MaxSpeed;
        
        [JsonProperty(Order = 6), DefaultValue(DefaultValues.MaxBoostSpeed)]
        public float maxBoostSpeed = DefaultValues.MaxBoostSpeed;
        
        [JsonProperty(Order = 7), DefaultValue(DefaultValues.MaxThrust)]
        public float maxThrust = DefaultValues.MaxThrust;
        
        [JsonProperty(Order = 8), DefaultValue(DefaultValues.MaxAngularVelocity)]
        public float maxAngularVelocity = DefaultValues.MaxAngularVelocity;
        
        [JsonProperty(Order = 9), DefaultValue(DefaultValues.TorqueThrustMultiplier)]
        public float torqueThrustMultiplier = DefaultValues.TorqueThrustMultiplier;
        
        [JsonProperty(Order = 10), DefaultValue(DefaultValues.ThrottleMultiplier)]
        public float throttleMultiplier = DefaultValues.ThrottleMultiplier;
        
        [JsonProperty(Order = 11), DefaultValue(DefaultValues.LatHMultiplier)]
        public float latHMultiplier = DefaultValues.LatHMultiplier;
        
        [JsonProperty(Order = 12), DefaultValue(DefaultValues.LatVMultiplier)]
        public float latVMultiplier = DefaultValues.LatVMultiplier;
        
        [JsonProperty(Order = 13), DefaultValue(DefaultValues.PitchMultiplier)]
        public float pitchMultiplier = DefaultValues.PitchMultiplier;
        
        [JsonProperty(Order = 14), DefaultValue(DefaultValues.RollMultiplier)]
        public float rollMultiplier = DefaultValues.RollMultiplier;
        
        [JsonProperty(Order = 15), DefaultValue(DefaultValues.YawMultiplier)]
        public float yawMultiplier = DefaultValues.YawMultiplier;

        [JsonProperty(Order = 16), DefaultValue(DefaultValues.ThrustBoostMultiplier)]
        public float thrustBoostMultiplier = DefaultValues.ThrustBoostMultiplier;
        
        [JsonProperty(Order = 17), DefaultValue(DefaultValues.TorqueBoostMultiplier)]
        public float torqueBoostMultiplier = DefaultValues.TorqueBoostMultiplier;
        
        [JsonProperty(Order = 18), DefaultValue(DefaultValues.BoostSpoolUpTime)]
        public float boostSpoolUpTime = DefaultValues.BoostSpoolUpTime;
        
        [JsonProperty(Order = 19), DefaultValue(DefaultValues.TotalBoostTime)]
        public float totalBoostTime = DefaultValues.TotalBoostTime;
        
        [JsonProperty(Order = 20), DefaultValue(DefaultValues.TotalBoostRotationalTime)]
        public float totalBoostRotationalTime = DefaultValues.TotalBoostRotationalTime;
        
        [JsonProperty(Order = 21), DefaultValue(DefaultValues.BoostMaxSpeedDropOffTime)]
        public float boostMaxSpeedDropOffTime = DefaultValues.BoostMaxSpeedDropOffTime;
        
        [JsonProperty(Order = 22), DefaultValue(DefaultValues.BoostRechargeTime)]
        public float boostRechargeTime = DefaultValues.BoostRechargeTime;
        
        [JsonProperty(Order = 23), DefaultValue(DefaultValues.BoostCapacitorPercentCost)]
        public float boostCapacitorPercentCost = DefaultValues.BoostCapacitorPercentCost;
        
        [JsonProperty(Order = 24), DefaultValue(DefaultValues.BoostCapacityPercentChargeRate)]
        public float boostCapacityPercentChargeRate = DefaultValues.BoostCapacityPercentChargeRate;
        
        [JsonProperty(Order = 25), DefaultValue(DefaultValues.BoostMaxDivertablePower)]
        public float boostMaxDivertablePower = DefaultValues.BoostMaxDivertablePower;
        
        [JsonProperty(Order = 26), DefaultValue(DefaultValues.MinUserLimitedVelocity)]
        public float minUserLimitedVelocity = DefaultValues.MinUserLimitedVelocity;

        [JsonProperty(Order = 27), DefaultValue(DefaultValues.UseAltBoosters)]
        public bool useAltBoosters = DefaultValues.UseAltBoosters;

        [JsonProperty(Order = 28), DefaultValue(DefaultValues.BoosterForceMultiplier)]
        public float boosterForceMultiplier = DefaultValues.BoosterForceMultiplier;

        [JsonProperty(Order = 29), DefaultValue(DefaultValues.BoosterVelocityMultiplier)]
        public float boosterVelocityMultiplier = DefaultValues.BoosterVelocityMultiplier;

        [JsonProperty(Order = 30), DefaultValue(DefaultValues.BoosterThrustMultiplier)]
        public float boosterThrustMultiplier = DefaultValues.BoosterThrustMultiplier;
        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [CanBeNull]
        public static ShipParameters FromJsonString(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<ShipParameters>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                return null;
            }
        }

        public static string ToHashString(ShipParameters shipParameters)
        {
            string hashString = "";

            JObject parameters = JObject.Parse(shipParameters.ToJsonString());
            JObject defaults = JObject.Parse(ShipParameters.Defaults.ToJsonString());

            foreach (JProperty parameter in defaults.Properties())
            {
                if (!JToken.DeepEquals(parameters[parameter.Name], defaults[parameter.Name]))
                    hashString +=  parameter.Name + parameters[parameter.Name].ToString();
            }

            return hashString;
        }
    }
}