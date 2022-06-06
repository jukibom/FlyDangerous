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

        /**
         * Given a set of flight inputs, return new overrides based on auto-rotation.
         * lateralH, lateralV and throttle will be overwritten.
         * pitch, yaw and roll with be added to based on first three vector inputs. This allows a user to also
         * bind any of these axes individually if they so choose.
         * Setting drift to true disables lateral / vector changes.
         */
        public void UpdateShipFlightInput(ref float lateralH, ref float lateralV, ref float throttle, ref float pitch, ref float yaw, ref float roll,
            bool drift) {
            if (Preferences.Instance.GetBool("invertArcadeYAxis")) lateralV *= -1;

            // clamp
            lateralV = Math.Clamp(lateralV, -1f, 1f);
            lateralH = Math.Clamp(lateralH, -1f, 1f);
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

                // drift override - disable auto rotate to plane
                if (drift) rotationResolutionBlendFactor = 1;

                targetTransform.rotation = Quaternion.Lerp(planeRotation, freeRotation, rotationResolutionBlendFactor);

                var pitchRotate = MathfExtensions.Remap(-1, 1, -maxTargetRotationDegrees, maxTargetRotationDegrees, lateralV);
                var yawRotate = MathfExtensions.Remap(-1, 1, -maxTargetRotationDegrees, maxTargetRotationDegrees, lateralH);
                var rollRotate = Preferences.Instance.GetBool("autoShipRoll") ? yawRotate * -1 : 0;

                // apply an auto roll to the transform when pitch / yaw-ing
                targetTransform.Rotate(pitchRotate * -1, yawRotate, rollRotate);

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
                var yOffset = Mathf.Sin(zRotationFromPlane * Mathf.Deg2Rad) * (lateralH * drawCubePositionDistance);

                var freeLocalPosition = new Vector3(lateralH, lateralV, throttle) * drawCubePositionDistance;
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

            if (drift) {
                inputTranslation.x = 0;
                inputTranslation.y = 0;
            }

            lateralH = inputTranslation.x;
            lateralV = inputTranslation.y;
            throttle = inputTranslation.z;
            pitch += inputRotation.x * -1;
            yaw += inputRotation.y;
            if (!drift) roll += inputRotation.z * -1;

            #endregion
        }
    }
}