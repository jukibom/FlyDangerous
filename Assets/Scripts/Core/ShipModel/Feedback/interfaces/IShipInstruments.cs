using Core.ShipModel.ShipIndicator;

namespace Core.ShipModel.Feedback.interfaces {
    public interface IShipInstruments {
        public void OnShipIndicatorUpdate(IShipInstrumentData shipInstrumentData);
    }
}