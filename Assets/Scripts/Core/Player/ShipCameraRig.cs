using System;
using System.Collections.Generic;
using Cinemachine;
using Gameplay;
using UnityEngine;

namespace Core.Player {
    public class ShipCameraRig : MonoBehaviour {
        
        [SerializeField] public List<ShipCamera> cameras;
        public ShipCamera ActiveCamera { get; private set; }

        private void Start() {
            // Set active camera to preference
            // TODO: preference saving
            SetActiveCamera(cameras[0]);
        }

        public void UpdateCameras(Vector3 velocity, float maxVelocity, Vector3 force, float maxForce) {
            ActiveCamera.UpdateFov(velocity, maxVelocity);
            var offset = ActiveCamera.GetCameraOffset(force, maxForce);
            // Debug.Log(offset);
        }

        public void ToggleActiveCamera() {
            var index = cameras.IndexOf(ActiveCamera);
            SetActiveCamera(index == cameras.Count - 1 ? cameras[0] : cameras[index + 1]);
        } 
        
        private void SetActiveCamera(ShipCamera newCamera) {
            ActiveCamera = newCamera;
            ActiveCamera.Camera.MoveToTopOfPrioritySubqueue();
        }
    }
}