using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace Core.Player {
    public class ShipCameraController : MonoBehaviour {
        
        [SerializeField] public List<CinemachineVirtualCamera> cameras;
        public CinemachineVirtualCamera ActiveCamera { get; private set; }

        private void Start() {
            // Set active camera to preference
            // TODO: preference saving
            SetActiveCamera(cameras[0]);
        }

        public void ToggleActiveCamera() {
            var index = cameras.IndexOf(ActiveCamera);
            SetActiveCamera(index == cameras.Count - 1 ? cameras[0] : cameras[index + 1]);
        } 
        
        private void SetActiveCamera(CinemachineVirtualCamera newCamera) {
            ActiveCamera = newCamera;
            ActiveCamera.MoveToTopOfPrioritySubqueue();
        }
    }
}