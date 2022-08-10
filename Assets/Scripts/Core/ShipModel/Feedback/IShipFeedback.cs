using UnityEngine;

namespace Core.ShipModel.Feedback {
    /**
     * Interface describing current events for the purpose of third party feedback hardware
     */
    public interface IShipFeedback {
        /** A collision occuring with a force between 0 and 1 and direction with respect to the forward vector */ 
        void Collision(float force, Vector3 direction);
        
        /** A boost action initiated where the ship is gearing up for a boost along with a 0-1 progress toward it and a bool for the first frame*/
        void BoostDrop(float progress, bool firstFrame);

        /** A boost action with a 0-1 progress and a bool for the first frame*/
        void Boost(float progress, bool firstFrame);

        /** A 0-1 value of how much random noise ship shaking is occurring */
        void ShipShake(float shake);
    }
}