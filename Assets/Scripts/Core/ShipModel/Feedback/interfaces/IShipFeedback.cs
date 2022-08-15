namespace Core.ShipModel.Feedback.interfaces {
    /**
     * Interface describing current events for the purpose of third party feedback hardware
     */
    public interface IShipFeedback {
        void OnShipFeedbackUpdate(IShipFeedbackData shipFeedbackData);
    }
}