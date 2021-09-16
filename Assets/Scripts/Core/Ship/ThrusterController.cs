using System;
using System.Collections.Generic;
using Misc;
using UnityEngine;

namespace Core.Ship {
    public class ThrusterController : MonoBehaviour {
        [SerializeField] private List<Thruster> forwardThrusters;
        [SerializeField] private List<Thruster> reverseThrusters;
        [SerializeField] private List<Thruster> upThrusters;
        [SerializeField] private List<Thruster> downThrusters;
        [SerializeField] private List<Thruster> leftThrusters;
        [SerializeField] private List<Thruster> rightThrusters;

        [SerializeField] private List<Thruster> pitchUpThrusters;
        [SerializeField] private List<Thruster> pitchDownThrusters;
        [SerializeField] private List<Thruster> rollLeftThrusters;
        [SerializeField] private List<Thruster> rollRightThrusters;
        [SerializeField] private List<Thruster> yawLeftThrusters;
        [SerializeField] private List<Thruster> yawRightThrusters;
        
        private float targetForwardThrust;
        private float targetUpThrust;
        private float targetRightThrust;
        private float targetPitchThrust;
        private float targetRollThrust;
        private float targetYawThrust;

        [SerializeField] float baseForwardThrustScaleZ;
        [SerializeField] float boostForwardThrustScaleZ;
        
        public void AnimateBoostThrusters() {
            forwardThrusters.ForEach(thruster => {
                var thrusterTransform = thruster.transform;
                var localScale = thrusterTransform.localScale;
                localScale = new Vector3(
                    localScale.x, 
                    localScale.y,
                    boostForwardThrustScaleZ
                );
                thrusterTransform.localScale = localScale;
            });
        }
        
        public void UpdateThrusters(Vector3 lateralThrust, Vector3 rotationalThrust) {
            
            targetForwardThrust = MathfExtensions.Clamp(-1, 1, lateralThrust.z);
            targetUpThrust = MathfExtensions.Clamp(-1, 1, lateralThrust.y);
            targetRightThrust = MathfExtensions.Clamp(-1, 1, lateralThrust.x);
            targetPitchThrust = MathfExtensions.Clamp(-1, 1, -rotationalThrust.x);
            targetRollThrust = MathfExtensions.Clamp(-1, 1, rotationalThrust.z);
            targetYawThrust = MathfExtensions.Clamp(-1, 1, rotationalThrust.y);
            
            // reset our thrusters as operations from here on are additive
            ForEachThruster(thruster => thruster.TargetThrust = 0);

            DistributeThrust(forwardThrusters, reverseThrusters, targetForwardThrust);
            DistributeThrust(rightThrusters, leftThrusters, targetRightThrust);
            DistributeThrust(upThrusters, downThrusters, targetUpThrust);
            
            // torque isn't always neat -1:1
            DistributeThrust(pitchUpThrusters, pitchDownThrusters, targetPitchThrust);
            DistributeThrust(rollLeftThrusters, rollRightThrusters, targetRollThrust);
            DistributeThrust(yawRightThrusters, yawLeftThrusters, targetYawThrust);
        }

        private void FixedUpdate() {
            forwardThrusters.ForEach(thruster => {
                var thrusterTransform = thruster.transform;
                var localScale = thrusterTransform.localScale;
                localScale = new Vector3(
                    localScale.x, 
                    localScale.y,
                    Mathf.Lerp(localScale.z,  baseForwardThrustScaleZ, 0.01f)
                );
                thrusterTransform.localScale = localScale;
            });
        }

        private void DistributeThrust(List<Thruster> positiveThrusters, List<Thruster> negativeThrusters,
            float thrust) {
            if (thrust > 0) {
                positiveThrusters.ForEach(thruster => thruster.TargetThrust += thrust);
            }
            else {
                negativeThrusters.ForEach(thruster => thruster.TargetThrust += Math.Abs(thrust));
            }
        }

        private void ForEachThruster(Action<Thruster> action) {
            forwardThrusters.ForEach(action);
            reverseThrusters.ForEach(action);
            upThrusters.ForEach(action);
            downThrusters.ForEach(action);
            leftThrusters.ForEach(action);
            rightThrusters.ForEach(action);
            
            pitchUpThrusters.ForEach(action);
            pitchDownThrusters.ForEach(action);
            rollLeftThrusters.ForEach(action);
            rollRightThrusters.ForEach(action);
            yawLeftThrusters.ForEach(action);
            yawRightThrusters.ForEach(action);
        }
    }
}