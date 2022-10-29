using Core.Player;
using Core.ShipModel.Feedback.interfaces;
using UnityEngine;

namespace Core.ShipModel {
    public enum AssistToggleType {
        Vector,
        Rotational,
        Both
    }

    /**
     * Interface for various kinds of ships. This is updated from the Ship Player - some of which occurs via network
     * commands (those marked as network aware) and some of which on the local client only.
     */
    public interface IShipModel : IShipInstruments, IShipMotion, IShipFeedback {
        public ShipShake ShipShake { get; }
        public ShipCameraRig ShipCameraRig { get; set; }
        public MonoBehaviour Entity();

        public void SetVisible(bool visible);

        public void SetIsLocalPlayer(bool isLocalPlayer);

        /**
         * Enable the night vision mode.
         * This function is network aware.
         */
        public void SetNightVision(bool active);

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
        public void Boost(float spoolTime, float boostTime);

        /**
         * Cancel the boost operation due to a collision
         */
        public void BoostCancel();

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

        /**
         * Ship is colliding with a holographic billboard
         */
        public void BillboardCollision();
    }
}