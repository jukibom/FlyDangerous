using Core.ShipModel.ShipIndicator;

namespace Core.ShipModel.Feedback.interfaces {
    public interface IShipInstruments {
        public void OnShipInstrumentUpdate(IShipInstrumentData shipInstrumentData);
    }
}