using System.Globalization;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Ship {
    public class Puffin : MonoBehaviour, IShip<MonoBehaviour>{
        
        [SerializeField] private Text velocityIndicator;
        [SerializeField] private Image accelerationBar;
        [SerializeField] private Text boostIndicator;
        [SerializeField] private Image boostCapacitorBar;
        [SerializeField] private Light shipLights;
        [SerializeField] private GameObject cockpitInternal;
        [SerializeField] private GameObject cockpitExternal;
        [SerializeField] private ThrusterController thrusterController;

        public MonoBehaviour Entity() {
            return this;
        }
        
        public void ToggleLights() {
            // TODO: Move all sounds over D:  
            // AudioManager.Instance.Play("ui-nav");
            shipLights.enabled = !shipLights.enabled;
        }

        public void SetCockpitMode(CockpitMode cockpitMode) {
            cockpitInternal.SetActive(cockpitMode == CockpitMode.Internal);
            cockpitExternal.SetActive(cockpitMode == CockpitMode.External);
        }
        
        public void UpdateIndicators(ShipIndicatorData shipIndicatorData) {
            if (velocityIndicator != null) {
                velocityIndicator.text = shipIndicatorData.velocity.ToString(CultureInfo.InvariantCulture);
            }

            if (accelerationBar != null) {
                accelerationBar.fillAmount = MathfExtensions.Remap(0, 1, 0, 0.755f, shipIndicatorData.acceleration);
                accelerationBar.color = Color.Lerp(Color.green, Color.red, shipIndicatorData.acceleration);
            }

            if (boostIndicator != null) {
                boostIndicator.text = ((int) shipIndicatorData.boostCapacitorPercent).ToString(CultureInfo.InvariantCulture) + "%";
            }

            if (boostCapacitorBar != null) {
                boostCapacitorBar.fillAmount = MathfExtensions.Remap(0, 100, 0, 0.775f, shipIndicatorData.boostCapacitorPercent);
                boostCapacitorBar.color = Color.Lerp(Color.red, Color.green, shipIndicatorData.boostCapacitorPercent / 100);
            }
        }

        public void UpdateThrusters(Vector3 force, Vector3 torque) {
            thrusterController.UpdateThrusters(force, torque);
        }

        public void UpdateBoostState(bool boostStart, float boostProgress) {
            
        }
    }
}