using UnityEngine;

namespace Core.Ship {
    public class Calidris : MonoBehaviour, IShip<MonoBehaviour> {
        
        [SerializeField] private GameObject cockpitInternal;
        [SerializeField] private GameObject cockpitExternal;
        [SerializeField] private ThrusterController thrusterController;
        
        public MonoBehaviour Entity() {
            return this;
        }

        public void ToggleLights() {
            
        }

        public void SetCockpitMode(CockpitMode cockpitMode) {
            cockpitInternal.SetActive(cockpitMode == CockpitMode.Internal);
            cockpitExternal.SetActive(cockpitMode == CockpitMode.External);
        }

        public void UpdateIndicators(ShipIndicatorData shipIndicatorData) {
            
        }

        public void UpdateThrusters(Vector3 force, Vector3 torque) {
            thrusterController.UpdateThrusters(force, torque);
        }

        public void UpdateBoostState(bool boostStart, float boostProgress) {
            
        }
    }
}