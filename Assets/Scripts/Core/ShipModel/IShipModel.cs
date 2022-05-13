using UnityEngine;

namespace Core.ShipModel {
    public struct ShipIndicatorData {
        public float throttlePosition; // -1 - 1
        public float velocity; // m/s
        public float acceleration; // 0 - 1
        public float throttle; // 0 - 1
        public float gForce;
        public float boostCapacitorPercent; // 0 - 100
        public bool boostTimerReady;
        public bool boostChargeReady;
        public bool lightsActive;
        public bool velocityLimiterActive;
        public bool vectorFlightAssistActive;
        public bool rotationalFlightAssistActive;
    }

    public enum CockpitMode {
        Internal,
        External
    }

    public enum AssistToggleType {
        Vector,
        Rotational,
        Both
    }

    /**
     * Interface for various kinds of ships. This is updated from the Ship Player - some of which occurs via network
     * commands (those marked as network aware) and some of which on the local client only.
     */
    public interface IShipModel {
        public MonoBehaviour Entity();

        public void SetVisible(bool visible);

        /**
         * Enable the lights on the ship
         * This function is network aware.
         */
        public void SetLights(bool active);

        /**
         * Do something when enabling or disabling some form of assist
         */
        public void SetAssist(AssistToggleType assistToggleType, bool active);

        /**
         * Enable the limiter
         */
        public void SetVelocityLimiter(bool active);

        /**
         * Play boost sounds and any other needed visual effects
         * This function is network-aware.
         */
        public void Boost(float boostTime);

        /** Update the cockpit indicators (local player only, others not needed).*/
        public void UpdateIndicators(ShipIndicatorData shipIndicatorData);

        /**
         * Anything related to motion - thrusters, sounds etc - based on the velocity, force and torque of the player.
         * This function is network-aware.
         */
        public void UpdateMotionInformation(Vector3 velocity, float maxVelocity, Vector3 force, Vector3 torque);

        /**
         * Set the main color of the ship as a html color
         */
        public void SetPrimaryColor(string htmlColor);

        /**
         * Set the accent color of the ship as a html color
         */
        public void SetAccentColor(string htmlColor);

        /**
         * Set the color of the individual thrusters visible on the outside of the ship
         */
        public void SetThrusterColor(string htmlColor);

        /**
         * Set the color of the trails which occur under boost
         */
        public void SetTrailColor(string htmlColor);

        /**
         * Set the color of the ship head-lights
         */
        public void SetHeadLightsColor(string htmlColor);
    }
}