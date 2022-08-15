using Core.ShipModel.Feedback.interfaces;
using UnityEngine;

namespace Core.ShipModel.Feedback {
    public class ShipFeedbackData : IShipFeedbackData {
        public bool CollisionThisFrame { get; set; }
        public float CollisionForceNormalised { get; set; }
        public Vector3 CollisionDirection { get; set; }
        public bool BoostDropStart { get; set; }
        public float BoostDropProgressNormalised { get; set; }
        public bool BoostStart { get; set; }
        public float BoostProgressNormalised { get; set; }
        public float ShipShake { get; set; }
    }
}