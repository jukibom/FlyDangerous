using System;
using Misc;
using UnityEngine;

namespace Core.Player {
    public class ShipArcadeFlightComputer : MonoBehaviour {
        [SerializeField] private Transform shipTransform;

        [SerializeField] private bool drawDebugCube;
        [Range(1, 10)] [SerializeField] private float drawCubePositionDistance = 10;

        [SerializeField] private bool translateTarget = true;
        [SerializeField] private bool rotateTarget = true;
        [SerializeField] private bool translateShip = true;
        [SerializeField] private bool rotateShip = true;
        private MeshRenderer _meshRenderer;

        private Transform _transform;
        [Range(0, 90)] [SerializeField] private float fixedToPlaneAngle = 0;
        [Range(0, 90)] [SerializeField] private float freeMoveAngle = 30;
        [Range(0, 1)] [SerializeField] private float planeTransformDamping = 0.8f;
        [Range(0, 90)] [SerializeField] private int maxTargetRotationDegrees = 45;
        private void Update() {
            _meshRenderer.enabled = drawDebugCube;
        }

        private void OnEnable() {
            _transform = GetComponent<Transform>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        public void UpdateShipFlightInput(ShipPlayer shipPlayer, float pitch, float yaw, float throttle) {
            // clamp
            pitch = Math.Clamp(pitch, -1f, 1f);
            yaw = Math.Clamp(yaw, -1f, 1f);
            throttle = Math.Clamp(throttle, -1f, 1f);

            var shipRotation = shipTransform.rotation;
            var shipRotEuler = shipRotation.eulerAngles;


            // Trend the transform roll back to 0 (world) when the ship is oriented on the world plane xz
            // Bear with me because there's some freaking big brain energy going on here.
            // A plane fixed rotation has the ship attempting to get to a flat roll - it can yaw and pitch freely.
            // A free rotation is anything goes in the context of local rotation.
            // What we're doing here is blending between the two states based on the angle of incidence - this allows
            // the ship to go up and down freely without breaking at the poles but the more the player brings it back 
            // into line, the more it will be pulled into a Y-UP orientation.
            if (rotateTarget) {
                var planeRotation = Quaternion.Euler(shipRotEuler.x, shipRotEuler.y, 0);
                var freeRotation = shipRotation;
                _transform.rotation = Quaternion.Lerp(planeRotation, freeRotation,
                    MathfExtensions.Remap(fixedToPlaneAngle, freeMoveAngle, planeTransformDamping, 1,
                        Mathf.Abs(Mathf.DeltaAngle(0, shipRotEuler.x))));

                // apply an auto roll to the transform when pitch / yaw-ing
                var pitchRotate = MathfExtensions.Remap(-1, 1, -maxTargetRotationDegrees, maxTargetRotationDegrees, pitch);
                var yawRotate = MathfExtensions.Remap(-1, 1, -maxTargetRotationDegrees, maxTargetRotationDegrees, yaw);
                _transform.Rotate(pitchRotate * -1, yawRotate, yawRotate * -1);
            }

            var localRotation = _transform.localRotation;
            var deltaXRot = Mathf.DeltaAngle(0, localRotation.eulerAngles.x);
            var deltaYRot = Mathf.DeltaAngle(0, localRotation.eulerAngles.y);
            var deltaZRot = Mathf.DeltaAngle(0, localRotation.eulerAngles.z);


            // move the transform
            if (translateTarget) {
                var freeLocalPosition = new Vector3(yaw, pitch, throttle) * drawCubePositionDistance;

                var zRotationFromPlane = Mathf.DeltaAngle(0, shipTransform.localRotation.eulerAngles.z);
                var yOffset = Mathf.Sin(zRotationFromPlane * Mathf.Deg2Rad) * (yaw * drawCubePositionDistance);
                var planeLocalPosition =
                    new Vector3(freeLocalPosition.x, freeLocalPosition.y - yOffset, freeLocalPosition.z);

                _transform.localPosition = Vector3.Lerp(planeLocalPosition, freeLocalPosition,
                    MathfExtensions.Remap(fixedToPlaneAngle, freeMoveAngle, planeTransformDamping, 1,
                        Mathf.Abs(Mathf.DeltaAngle(0, shipRotEuler.x))));
            }

            var localPosition = _transform.localPosition;


            // apply input to the ship in the direction of the transform
            if (translateShip) {
                shipPlayer.SetLateralH(localPosition.x / drawCubePositionDistance);
                shipPlayer.SetLateralV(localPosition.y / drawCubePositionDistance);
                shipPlayer.SetThrottle(localPosition.z / drawCubePositionDistance);
            }

            if (rotateShip) {
                shipPlayer.SetPitch(deltaXRot / maxTargetRotationDegrees * -1);
                shipPlayer.SetYaw(deltaYRot / maxTargetRotationDegrees);
                shipPlayer.SetRoll(deltaZRot / maxTargetRotationDegrees * -1);
            }
        }
    }
}