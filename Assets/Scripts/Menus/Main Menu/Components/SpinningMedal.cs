using NaughtyAttributes;
using UnityEngine;

namespace Menus.Main_Menu.Components {
    public class SpinningMedal : MonoBehaviour {
        [SerializeField] private GameObject medalModel;

        [Label("Rotation (Degrees / Second)")] [SerializeField]
        private float rotationDegrees = 180;

        [Label("Sync with transform on enable (optional)")] [SerializeField]
        private Transform syncWith;

        private void Update() {
            var modelTransform = medalModel.transform;
            modelTransform.localPosition = Vector3.zero;
            modelTransform.RotateAround(modelTransform.position, Vector3.up, rotationDegrees * Time.deltaTime);
        }

        private void OnEnable() {
            if (syncWith != null) medalModel.transform.localRotation = syncWith.localRotation;
            medalModel.transform.RotateAround(medalModel.transform.position, Vector3.up, -20);
        }
    }
}