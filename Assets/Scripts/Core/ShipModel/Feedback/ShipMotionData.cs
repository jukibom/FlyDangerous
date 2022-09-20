using Core.ShipModel.Feedback.interfaces;
using UnityEngine;

namespace Core.ShipModel.Feedback {
    public class ShipMotionData : IShipMotionData {
        public Vector3 CurrentLateralVelocity { get; set; }
        public Vector3 CurrentLateralForce { get; set; }
        public Vector3 CurrentAngularVelocity { get; set; }
        public Vector3 CurrentAngularTorque { get; set; }
        public Vector3 CurrentLateralVelocityNormalised { get; set; }
        public Vector3 CurrentLateralForceNormalised { get; set; }
        public Vector3 CurrentAngularVelocityNormalised { get; set; }
        public Vector3 CurrentAngularTorqueNormalised { get; set; }
        public Vector3 WorldRotationEuler { get; set; }
        public float MaxSpeed { get; set; }
    }
}