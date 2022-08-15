using Core.ShipModel.ShipIndicator;

namespace Core.ShipModel.Feedback.interfaces {
    public interface IShipIndicators {
        public void OnShipIndicatorUpdate(IShipIndicatorData shipIndicatorData);
    }
}