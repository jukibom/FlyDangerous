using UnityEngine;

namespace Core.Ship {
    
    public struct ShipIndicatorData {
        public float velocity; // m/s
        public float acceleration; // 0-1
        public float throttle; // 0-1
        public float boostCapacitorPercent; // 0-100
    }

    public enum CockpitMode {
        Internal,
        External,
    }
    
    public interface IShip<T> {
        public T Entity();
        
        /** Enable the lights on the model */
        public void ToggleLights();
        
        /** Set the visible entity for internal vs external cam (stacking means need to pick one!) */
        public void SetCockpitMode(CockpitMode cockpitMode);
        
        /** Update the cockpit indicators (local player only, others not needed) */
        public void UpdateIndicators(ShipIndicatorData shipIndicatorData);
        
        /** Draw the thrusters based on the force and torque of the player */
        public void UpdateThrusters(Vector3 force, Vector3 torque);
        
        /** Play boost sounds and any other needed visual effects */
        public void UpdateBoostState(bool boostStart, float boostProgress);
    }
}