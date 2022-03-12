using System;
using Misc;
using UnityEngine;

namespace Core.Player {
    public class ShipArcadeFlightComputer : MonoBehaviour {
        [SerializeField] private Transform shipTransform;
        [SerializeField] private Transform targetTransform;

        [SerializeField] private bool drawDebugCubes;
        [SerializeField] private Transform planeRotationDebugCube;
        [SerializeField] private Transform freeRotationDebugCube;

        [Range(1, 10)] [SerializeField] private float drawCubePositionDistance = 5;

        [SerializeField] private bool translateTarget = true;
        [SerializeField] private bool rotateTarget = true;
        [SerializeField] private bool translateShip = true;
        [SerializeField] private bool rotateShip = true;
        [Range(0, 89.9f)] [SerializeField] private float fixedToPlaneAngle;
        [Range(0.1f, 90)] [SerializeField] private float freeMoveAngle = 30;
        [Range(0, 1)] [SerializeField] private float planeTransformDamping = 0.8f;
        [Range(0, 90)] [SerializeField] private int maxTargetRotationDegrees = 45;
        private MeshRenderer _meshRenderer;

        private void Update() {
            _meshRenderer.enabled = drawDebugCubes;
            planeRotationDebugCube.gameObject.SetActive(drawDebugCubes);
            freeRotationDebugCube.gameObject.SetActive(drawDebugCubes);
        }

        private void OnEnable() {
            _meshRenderer = targetTransform.gameObject.GetComponent<MeshRenderer>();
        }

        public void UpdateShipFlightInput(ShipPlayer shipPlayer, float pitch, float yaw, float throttle) {
            // clamp
            pitch = Math.Clamp(pitch, -1f, 1f);
            yaw = Math.Clamp(yaw, -1f, 1f);
            throttle = Math.Clamp(throttle, -1f, 1f);

            var shipRotation = shipTransform.rotation;
            var shipRotEuler = shipRotation.eulerAngles;

            // blending is based primarily on ship angle from the world "ground" plane
            var shipAngleFromPlane = Mathf.Abs(Mathf.DeltaAngle(0, shipRotEuler.x));

            #region Rotation

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
                var rotationResolutionBlendFactor = MathfExtensions.Remap(fixedToPlaneAngle, freeMoveAngle, planeTransformDamping, 1, shipAngleFromPlane);
                targetTransform.rotation = Quaternion.Lerp(planeRotation, freeRotation, rotationResolutionBlendFactor);

                var pitchRotate = MathfExtensions.Remap(-1, 1, -maxTargetRotationDegrees, maxTargetRotationDegrees, pitch);
                var yawRotate = MathfExtensions.Remap(-1, 1, -maxTargetRotationDegrees, maxTargetRotationDegrees, yaw);

                // apply an auto roll to the transform when pitch / yaw-ing
                targetTransform.Rotate(pitchRotate * -1, yawRotate, yawRotate * -1);

                if (drawDebugCubes) {
                    planeRotationDebugCube.rotation = planeRotation;
                    freeRotationDebugCube.rotation = freeRotation;
                    planeRotationDebugCube.Rotate(pitchRotate * -1, yawRotate, yawRotate * -1);
                    freeRotationDebugCube.Rotate(pitchRotate * -1, yawRotate, yawRotate * -1);
                }
            }

            var localRotation = targetTransform.localRotation;
            var deltaRotation = new Vector3(
                Mathf.DeltaAngle(0, localRotation.eulerAngles.x),
                Mathf.DeltaAngle(0, localRotation.eulerAngles.y),
                Mathf.DeltaAngle(0, localRotation.eulerAngles.z)
            );

            #endregion

            #region Translation

            if (translateTarget) {
                var zRotationFromPlane = Mathf.DeltaAngle(0, shipTransform.localRotation.eulerAngles.z);
                var yOffset = Mathf.Sin(zRotationFromPlane * Mathf.Deg2Rad) * (yaw * drawCubePositionDistance);

                var freeLocalPosition = new Vector3(yaw, pitch, throttle) * drawCubePositionDistance;
                var planeLocalPosition = new Vector3(freeLocalPosition.x, freeLocalPosition.y - yOffset, freeLocalPosition.z);

                var positionResolutionBlendFactor = MathfExtensions.Remap(fixedToPlaneAngle, freeMoveAngle / 2, planeTransformDamping, 1, shipAngleFromPlane);
                targetTransform.localPosition = Vector3.Lerp(planeLocalPosition, freeLocalPosition, positionResolutionBlendFactor);

                if (drawDebugCubes) {
                    planeRotationDebugCube.localPosition = planeLocalPosition * 0.66f;
                    freeRotationDebugCube.localPosition = freeLocalPosition * 0.33f;
                }
            }

            var localPosition = targetTransform.localPosition;

            #endregion


            #region input

            // apply input to the ship in the direction of the transform
            var inputTranslation = Vector3.zero;
            var inputRotation = Vector3.zero;

            if (translateShip) inputTranslation = localPosition / drawCubePositionDistance;
            if (rotateShip) inputRotation = deltaRotation / maxTargetRotationDegrees;

            shipPlayer.SetLateralH(inputTranslation.x);
            shipPlayer.SetLateralV(inputTranslation.y);
            shipPlayer.SetThrottle(inputTranslation.z);
            shipPlayer.SetPitch(inputRotation.x * -1);
            shipPlayer.SetYaw(inputRotation.y);
            shipPlayer.SetRoll(inputRotation.z * -1);

            #endregion
        }
    }
}