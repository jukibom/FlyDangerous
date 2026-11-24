using Core.Player;
using UnityEngine;

namespace FlyDangerous.LevelEditor {
    public class EditorController : MonoBehaviour {
        
        [SerializeField] private EditorPresence editorPresence;
        
        public void Activate() {
            FdPlayer.LocalShipPlayer.User.TargetTransform = transform;
            FdPlayer.LocalShipPlayer.User.ShipCameraRig.ActiveCamera?.Reset();
        }

        public void Deactivate() {
            FdPlayer.LocalShipPlayer.User.TargetTransform = FdPlayer.LocalShipPlayer.transform;
            FdPlayer.LocalShipPlayer.User.ShipCameraRig.ActiveCamera?.Reset();
        }
    }
}