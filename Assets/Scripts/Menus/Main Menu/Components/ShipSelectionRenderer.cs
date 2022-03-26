using Core.ShipModel;
using Misc;
using UnityEngine;

namespace Menus.Main_Menu.Components {
    public class ShipSelectionRenderer : MonoBehaviour {
        [SerializeField] private Vector3 targetScale;
        private readonly Vector3 _rotation = new(0, 0.5f, 0);

        private GameObject _loadedShip;

        private void FixedUpdate() {
            transform.Rotate(_rotation);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 0.01f);
        }

        public void SetShip(ShipMeta shipData) {
            if (_loadedShip) Destroy(_loadedShip);

            _loadedShip = Instantiate(Resources.Load(shipData.PrefabToLoad, typeof(GameObject)) as GameObject);

            if (_loadedShip != null) {
                _loadedShip.transform.SetParent(transform, false);
                var layer = LayerMask.NameToLayer("UI3D");
                _loadedShip.gameObject.layer = layer;
                foreach (Transform child in _loadedShip.transform) child.SetLayer(layer);
                transform.localScale = Vector3.zero;
                // TODO: measure ship and set an appropriate scale. This works well for the two ships we have now.
            }
        }

        public void SetShipPrimaryColor(string htmlColor) {
            _loadedShip.GetComponent<IShipModel>().SetPrimaryColor(htmlColor);
        }

        public void SetShipAccentColor(string htmlColor) {
            _loadedShip.GetComponent<IShipModel>().SetAccentColor(htmlColor);
        }
    }
}