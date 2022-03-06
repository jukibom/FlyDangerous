using System;
using Cinemachine;
using Core;
using Core.Player;
using JetBrains.Annotations;
using Misc;
using UnityEngine;

namespace Gameplay {

    public enum CameraType {
        FirstPerson,
        ThirdPerson
    }
    
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class ShipCamera : MonoBehaviour {

        [SerializeField] public CameraType cameraType;
        
        // public float smoothSpeed = 0.5f;
        // public float accelerationDampener = 5f;
        //
        // private Vector3 _velocity = Vector3.zero;
        // private Vector3 _lastVelocity;
        //
        // public Vector3 minPos = new Vector3(-0.1175f, -0.0678f, -0.2856f);
        // public Vector3 maxPos = new Vector3(0.1175f, 0.04f, 0.0412f);

        private float _baseFov;
        private CinemachineVirtualCamera _camera;

        public CinemachineVirtualCamera Camera {
            get {
                if (_camera == null) {
                    _camera = GetComponent<CinemachineVirtualCamera>();
                }
                return _camera;
            }
        }

        public void OnEnable() {
            Game.OnGameSettingsApplied += SetBaseFov;
            SetBaseFov();
        }
    
        public void OnDisable() {
            Game.OnGameSettingsApplied -= SetBaseFov;
        }

        // public void Reset() {
        //     _lastVelocity = Vector3.zero;
        //     _lastVelocity = Vector3.zero;
        //     transform.localPosition = Vector3.zero;
        // }

        public void UpdateFov(Vector3 velocity, float maxVelocity) {
            Camera.m_Lens.FieldOfView = Mathf.Lerp(Camera.m_Lens.FieldOfView,
                MathfExtensions.Remap(0, 1, _baseFov, _baseFov + 20, velocity.z / maxVelocity), 
                0.1f
            );
        }
        
        public Vector3 GetCameraOffset(Vector3 force, float maxForce) {
            
            return Vector3.zero;
        }

        private void SetBaseFov() {
            _baseFov = cameraType == CameraType.FirstPerson
                ? Preferences.Instance.GetFloat("graphics-field-of-view")
                : Preferences.Instance.GetFloat("graphics-field-of-view-ext");
        }
    }
}
