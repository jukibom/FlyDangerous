using Core.Player;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ShipModel {
    public class IndicatorSystem : MonoBehaviour {
        [SerializeField] private CanvasGroup tviIndicators;
        [SerializeField] private Image tviForward;
        [SerializeField] private Image tviReverse;
        [SerializeField] private float tviAlphaSmoothing = 0.5f;
        [SerializeField] private float tviPositionalSmoothing = 0.5f;
        [SerializeField] private int indicatorDistance = 500;

        private Camera _mainCamera;
        private readonly Vector3 stationaryDirectionVector = new(0, 0, 1);
        private Vector3 lookAtUpVector;
        private Vector3 targetTVIForwardPosition;
        private Vector3 targetTVIReversePosition;
        private float targetTVIAlpha;

        private void FixedUpdate() {
            // update camera if needed
            if (_mainCamera == null || _mainCamera.enabled == false || _mainCamera.gameObject.activeSelf == false)
                _mainCamera = Camera.main;

            if (Preferences.Instance.GetBool("showTrueVectorIndicator"))
                UpdateTVIs();
            else
                targetTVIAlpha = 0;
        }

        private void Update() {
            if (_mainCamera == null) return;

            var tviForwardTransform = tviForward.transform;
            var tviReverseTransform = tviReverse.transform;
            var tviForwardLocalPosition = tviForwardTransform.localPosition;
            var tviReverseLocalPosition = tviReverseTransform.localPosition;

            // interpolate values
            tviIndicators.alpha = Mathf.Lerp(tviIndicators.alpha, targetTVIAlpha, tviAlphaSmoothing);
            tviForwardLocalPosition = Vector3.Lerp(tviForwardLocalPosition, targetTVIForwardPosition, tviPositionalSmoothing);
            tviReverseLocalPosition = Vector3.Lerp(tviReverseLocalPosition, targetTVIReversePosition, tviPositionalSmoothing);

            // make sure the indicators are always at the required distance away on the sphere (may interpolate through the camera otherwise!)
            tviForwardTransform.localPosition = tviForwardLocalPosition.normalized * indicatorDistance;
            tviReverseTransform.localPosition = tviReverseLocalPosition.normalized * indicatorDistance;

            var mainCameraTransform = _mainCamera.transform;
            tviForwardTransform.LookAt(mainCameraTransform, lookAtUpVector);
            tviReverseTransform.LookAt(mainCameraTransform, lookAtUpVector);
        }

        private void UpdateTVIs() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player && _mainCamera) {
                var shipTransform = player.transform;

                var velocity = player.ShipPhysics.Velocity;
                var shipDirectionVector = shipTransform.InverseTransformDirection(velocity.normalized);

                // dirty cludge for flight assist 
                var positionVector = shipDirectionVector;
                if (player.ShipPhysics.VectorFlightAssistActive)
                    positionVector = Vector3.Lerp(stationaryDirectionVector, shipDirectionVector, velocity.magnitude.Remap(0, 100, 0, 1));

                targetTVIForwardPosition = positionVector * indicatorDistance;
                targetTVIReversePosition = positionVector * (indicatorDistance * -1);

                // Fade out the indicators when either slowing down or interpolating the position inverse (i.e. when the indicator is getting closer which it really shouldn't)
                targetTVIAlpha = velocity.magnitude.Remap(2, 10, 0, 1);

                lookAtUpVector = Game.IsVREnabled ? player.transform.up : _mainCamera.transform.up;
            }
        }
    }
}