using System.Numerics;

namespace Core.ShipModel.Feedback {
    /**
     * Interface describing the current ship motion for the purpose of third party integration of simulators, OSDs etc
     */
    public interface IShipMotion {
        /** The normalised (of max) current velocity of the ship in local space */
        void CurrentLateralVelocity(Vector3 velocity);

        /** The normalised (of max) current acceleration force applied in local space */
        void CurrentLateralForce(Vector3 force);

        /** The normalised (of max) current rotational velocity in local space */
        void CurrentAngularVelocity(Vector3 angularVelocity);

        /** The normalised (of max) current rotational torque in local space */
        void CurrentAngularTorque(Vector3 angularTorque);

        /** All additional data used to display UI elements etc for the current ship status (boost %, G-Force etc) */
        void CurrentShipIndicatorData(ShipIndicatorData indicatorData);
    }
}