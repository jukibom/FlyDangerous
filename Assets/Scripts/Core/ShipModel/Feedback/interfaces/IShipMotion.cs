namespace Core.ShipModel.Feedback.interfaces {
    /**
     * Interface describing the current ship motion for the purpose of third party integration of simulators, OSDs etc
     */
    public interface IShipMotion {
        void OnShipMotionUpdate(IShipMotionData shipMotionData);
    }
}