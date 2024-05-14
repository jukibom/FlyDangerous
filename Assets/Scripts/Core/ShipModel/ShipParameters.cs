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
                return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [CanBeNull]
        public static ShipParameters FromJsonString(string json) {
            try {
                ShipParameters returnedParameters = ShipParameters.Defaults;

                var parameters = JsonConvert.DeserializeObject<NullableParameters>(json);

                if (parameters.angularDrag != Defaults.angularDrag)
                    if (parameters.angularDrag == null)
                        returnedParameters.angularDrag = Defaults.angularDrag;
                    else returnedParameters.angularDrag = (float)parameters.angularDrag;

                if (parameters.boostCapacitorPercentCost != Defaults.boostCapacitorPercentCost)
                    if (parameters.boostCapacitorPercentCost == null)
                        returnedParameters.boostCapacitorPercentCost = Defaults.boostCapacitorPercentCost;
                    else returnedParameters.boostCapacitorPercentCost = (float)parameters.boostCapacitorPercentCost;

                if (parameters.boostCapacityPercentChargeRate != Defaults.boostCapacityPercentChargeRate)
                    if (parameters.boostCapacityPercentChargeRate == null)
                        returnedParameters.boostCapacityPercentChargeRate = Defaults.boostCapacityPercentChargeRate;
                    else returnedParameters.boostCapacityPercentChargeRate = (float)parameters.boostCapacityPercentChargeRate;

                if (parameters.boostMaxDivertablePower != Defaults.boostMaxDivertablePower)
                    if (parameters.boostMaxDivertablePower == null)
                        returnedParameters.boostMaxDivertablePower = Defaults.boostMaxDivertablePower;
                    else returnedParameters.boostMaxDivertablePower = (float)parameters.boostMaxDivertablePower;

                if (parameters.boostMaxSpeedDropOffTime != Defaults.boostMaxSpeedDropOffTime)
                    if (parameters.boostMaxSpeedDropOffTime == null)
                        returnedParameters.boostMaxSpeedDropOffTime = Defaults.boostMaxSpeedDropOffTime;
                    else returnedParameters.boostMaxSpeedDropOffTime = (float)parameters.boostMaxSpeedDropOffTime;

                if (parameters.boostRechargeTime != Defaults.boostRechargeTime)
                    if (parameters.boostRechargeTime == null)
                        returnedParameters.boostRechargeTime = Defaults.boostRechargeTime;
                    else returnedParameters.boostRechargeTime = (float)parameters.boostRechargeTime;

                if (parameters.boostSpoolUpTime != Defaults.boostSpoolUpTime)
                    if (parameters.boostSpoolUpTime == null)
                        returnedParameters.boostSpoolUpTime = Defaults.boostSpoolUpTime;
                    else returnedParameters.boostSpoolUpTime = (float)parameters.boostSpoolUpTime;

                if (parameters.drag != Defaults.drag)
                    if (parameters.drag == null)
                        returnedParameters.drag = Defaults.drag;
                    else returnedParameters.drag = (float)parameters.drag;

                if (parameters.inertiaTensorMultiplier != Defaults.inertiaTensorMultiplier)
                    if (parameters.inertiaTensorMultiplier == null)
                        returnedParameters.inertiaTensorMultiplier = Defaults.inertiaTensorMultiplier;
                    else returnedParameters.inertiaTensorMultiplier = (float)parameters.inertiaTensorMultiplier;

                if (parameters.latHMultiplier != Defaults.latHMultiplier)
                    if (parameters.latHMultiplier == null)
                        returnedParameters.latHMultiplier = Defaults.latHMultiplier;
                    else returnedParameters.latHMultiplier = (float)parameters.latHMultiplier;

                if (parameters.latVMultiplier != Defaults.latVMultiplier)
                    if (parameters.latVMultiplier == null)
                        returnedParameters.latVMultiplier = Defaults.latVMultiplier;
                    else returnedParameters.latVMultiplier = (float)parameters.latVMultiplier;

                if (parameters.mass != Defaults.mass)
                    if (parameters.mass == null)
                        returnedParameters.mass = Defaults.mass;
                    else returnedParameters.mass = (float)parameters.mass;

                if (parameters.maxAngularVelocity != Defaults.maxAngularVelocity)
                    if (parameters.maxAngularVelocity == null)
                        returnedParameters.maxAngularVelocity = Defaults.maxAngularVelocity;
                    else returnedParameters.maxAngularVelocity = (float)parameters.maxAngularVelocity;

                if (parameters.maxBoostSpeed != Defaults.maxBoostSpeed)
                    if (parameters.maxBoostSpeed == null)
                        returnedParameters.maxBoostSpeed = Defaults.maxBoostSpeed;
                    else returnedParameters.maxBoostSpeed = (float)parameters.maxBoostSpeed;

                if (parameters.maxSpeed != Defaults.maxSpeed)
                    if (parameters.maxSpeed == null)
                        returnedParameters.maxSpeed = Defaults.maxSpeed;
                    else returnedParameters.maxSpeed = (float)parameters.maxSpeed;

                if (parameters.maxThrust != Defaults.maxThrust)
                    if (parameters.maxThrust == null)
                        returnedParameters.maxThrust = Defaults.maxThrust;
                    else returnedParameters.maxThrust = (float)parameters.maxThrust;

                if (parameters.minUserLimitedVelocity != Defaults.minUserLimitedVelocity)
                    if (parameters.minUserLimitedVelocity == null)
                        returnedParameters.minUserLimitedVelocity = Defaults.minUserLimitedVelocity;
                    else returnedParameters.minUserLimitedVelocity = (float)parameters.minUserLimitedVelocity;

                if (parameters.pitchMultiplier != Defaults.pitchMultiplier)
                    if (parameters.pitchMultiplier == null)
                        returnedParameters.pitchMultiplier = Defaults.pitchMultiplier;
                    else returnedParameters.pitchMultiplier = (float)parameters.pitchMultiplier;

                if (parameters.rollMultiplier != Defaults.rollMultiplier)
                    if (parameters.rollMultiplier == null)
                        returnedParameters.rollMultiplier = Defaults.rollMultiplier;
                    else returnedParameters.rollMultiplier = (float)parameters.rollMultiplier;

                if (parameters.throttleMultiplier != Defaults.throttleMultiplier)
                    if (parameters.throttleMultiplier == null)
                        returnedParameters.throttleMultiplier = Defaults.throttleMultiplier;
                    else returnedParameters.throttleMultiplier = (float)parameters.throttleMultiplier;

                if (parameters.thrustBoostMultiplier != Defaults.thrustBoostMultiplier)
                    if (parameters.thrustBoostMultiplier == null)
                        returnedParameters.thrustBoostMultiplier = Defaults.thrustBoostMultiplier;
                    else returnedParameters.thrustBoostMultiplier = (float)parameters.thrustBoostMultiplier;

                if (parameters.torqueBoostMultiplier != Defaults.torqueBoostMultiplier)
                    if (parameters.torqueBoostMultiplier == null)
                        returnedParameters.torqueBoostMultiplier = Defaults.torqueBoostMultiplier;
                    else returnedParameters.torqueBoostMultiplier = (float)parameters.torqueBoostMultiplier;

                if (parameters.torqueThrustMultiplier != Defaults.torqueThrustMultiplier)
                    if (parameters.torqueThrustMultiplier == null)
                        returnedParameters.torqueThrustMultiplier = Defaults.torqueThrustMultiplier;
                    else returnedParameters.torqueThrustMultiplier = (float)parameters.torqueThrustMultiplier;

                if (parameters.totalBoostRotationalTime != Defaults.totalBoostRotationalTime)
                    if (parameters.totalBoostRotationalTime == null)
                        returnedParameters.totalBoostRotationalTime = Defaults.totalBoostRotationalTime;
                    else returnedParameters.totalBoostRotationalTime = (float)parameters.totalBoostRotationalTime;

                if (parameters.totalBoostTime != Defaults.totalBoostTime)
                    if (parameters.totalBoostTime == null)
                        returnedParameters.totalBoostTime = Defaults.totalBoostTime;
                    else returnedParameters.totalBoostTime = (float)parameters.totalBoostTime;

                if (parameters.yawMultiplier != Defaults.yawMultiplier)
                    if (parameters.yawMultiplier == null)
                        returnedParameters.yawMultiplier = Defaults.yawMultiplier;
                    else returnedParameters.yawMultiplier = (float)parameters.yawMultiplier;

                if (parameters.boostDivertEfficiency != Defaults.boostDivertEfficiency)
                    if (parameters.boostDivertEfficiency == null)
                        returnedParameters.boostDivertEfficiency = Defaults.boostDivertEfficiency;
                    else returnedParameters.boostDivertEfficiency = (float)parameters.boostDivertEfficiency;

                return returnedParameters;
            }
            catch (Exception e) {
                Debug.LogWarning(e.Message);
                return null;
            }
        }
    }
    public class NullableParameters
    {
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

        public static NullableParameters GetChangedParameters()
        {
            var parameters = JsonConvert.DeserializeObject<NullableParameters>(Game.Instance.ShipParameters.ToJsonString());

            if ((float)parameters.mass == ShipParameters.Defaults.mass)
                parameters.mass = null;

            if ((float)parameters.drag == ShipParameters.Defaults.drag)
                parameters.drag = null;

            if ((float)parameters.angularDrag == ShipParameters.Defaults.angularDrag)
                parameters.angularDrag = null;

            if ((float)parameters.inertiaTensorMultiplier == ShipParameters.Defaults.inertiaTensorMultiplier)
                parameters.inertiaTensorMultiplier = null;

            if ((float)parameters.maxSpeed == ShipParameters.Defaults.maxSpeed)
                parameters.maxSpeed = null;

            if ((float)parameters.maxBoostSpeed == ShipParameters.Defaults.maxBoostSpeed)
                parameters.maxBoostSpeed = null;

            if ((float)parameters.maxThrust == ShipParameters.Defaults.maxThrust)
                parameters.maxThrust = null;

            if ((float)parameters.maxAngularVelocity == ShipParameters.Defaults.maxAngularVelocity)
                parameters.maxAngularVelocity = null;

            if ((float)parameters.torqueThrustMultiplier == ShipParameters.Defaults.torqueThrustMultiplier)
                parameters.torqueThrustMultiplier = null;

            if ((float)parameters.throttleMultiplier == ShipParameters.Defaults.throttleMultiplier)
                parameters.throttleMultiplier = null;

            if ((float)parameters.latHMultiplier == ShipParameters.Defaults.latHMultiplier)
                parameters.latHMultiplier = null;

            if ((float)parameters.latVMultiplier == ShipParameters.Defaults.latVMultiplier)
                parameters.latVMultiplier = null;

            if ((float)parameters.pitchMultiplier == ShipParameters.Defaults.pitchMultiplier)
                parameters.pitchMultiplier = null;

            if ((float)parameters.rollMultiplier == ShipParameters.Defaults.rollMultiplier)
                parameters.rollMultiplier = null;

            if ((float)parameters.yawMultiplier == ShipParameters.Defaults.yawMultiplier)
                parameters.yawMultiplier = null;

            if ((float)parameters.thrustBoostMultiplier == ShipParameters.Defaults.thrustBoostMultiplier)
                parameters.thrustBoostMultiplier = null;

            if ((float)parameters.torqueBoostMultiplier == ShipParameters.Defaults.torqueBoostMultiplier)
                parameters.torqueBoostMultiplier = null;

            if ((float)parameters.boostSpoolUpTime == ShipParameters.Defaults.boostSpoolUpTime)
                parameters.boostSpoolUpTime = null;

            if ((float)parameters.totalBoostTime == ShipParameters.Defaults.totalBoostTime)
                parameters.totalBoostTime = null;

            if ((float)parameters.totalBoostRotationalTime == ShipParameters.Defaults.totalBoostRotationalTime)
                parameters.totalBoostRotationalTime = null;

            if ((float)parameters.boostMaxSpeedDropOffTime == ShipParameters.Defaults.boostMaxSpeedDropOffTime)
                parameters.boostMaxSpeedDropOffTime = null;

            if ((float)parameters.boostRechargeTime == ShipParameters.Defaults.boostRechargeTime)
                parameters.boostRechargeTime = null;

            if ((float)parameters.boostCapacitorPercentCost == ShipParameters.Defaults.boostCapacitorPercentCost)
                parameters.boostCapacitorPercentCost = null;

            if ((float)parameters.boostCapacityPercentChargeRate == ShipParameters.Defaults.boostCapacityPercentChargeRate)
                parameters.boostCapacityPercentChargeRate = null;

            if ((float)parameters.boostMaxDivertablePower == ShipParameters.Defaults.boostMaxDivertablePower)
                parameters.boostMaxDivertablePower = null;

            if ((float)parameters.minUserLimitedVelocity == ShipParameters.Defaults.minUserLimitedVelocity)
                parameters.minUserLimitedVelocity = null;

            if ((float)parameters.boostDivertEfficiency == ShipParameters.Defaults.boostDivertEfficiency)
                parameters.boostDivertEfficiency = null;


            return parameters;
        }
    }
}