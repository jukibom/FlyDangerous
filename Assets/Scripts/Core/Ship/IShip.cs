using UnityEngine;

namespace Core.Ship {
    
    public struct ShipIndicatorData {
        public float throttlePosition; // -1 - 1
        public float velocity; // m/s
        public float acceleration; // 0 - 1
        public float throttle; // 0 - 1
        public float gForce;
        public float boostCapacitorPercent; // 0 - 100
        public bool boostReady;
        public bool lightsActive;
        public bool velocityLimiterActive;
        public bool vectorFlightAssistActive;
        public bool rotationalFlightAssistActive;
    }

    public enum CockpitMode {
        Internal,
        External,
    }
    
    public interface IShip {
        public MonoBehaviour Entity();
        
        /** Enable the lights on the ship */
        public void SetLights(bool active);

        /** Do something when enabling or disabling some form of assist */
        public void SetAssist(bool active);
        
        /** Enable the limiter */
        public void SetVelocityLimiter(bool active);

        /** Play boost sounds and any other needed visual effects */
        public void Boost(float boostTime);

        /** Update the cockpit indicators (local player only, others not needed) */
        public void UpdateIndicators(ShipIndicatorData shipIndicatorData);
        
        /** Draw the thrusters based on the force and torque of the player */
        public void UpdateThrusters(Vector3 force, Vector3 torque);

        /** Set the visible entity for internal vs external cam (stacking means need to pick one!) */
        public void SetCockpitMode(CockpitMode cockpitMode);
    }
}