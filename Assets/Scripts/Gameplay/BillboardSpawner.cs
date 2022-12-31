using System.Collections.Generic;
using System.Linq;
using Core.MapData;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;

namespace Gameplay {
    [ExecuteAlways]
    [RequireComponent(typeof(Billboard))]
    public class BillboardSpawner : MonoBehaviour {
        [SerializeField] private Billboard billboard;

        [Dropdown("GetBillboardTypes")] [OnValueChanged("RefreshFromBillboardData")] [SerializeField]
        private string billboardType;

        [ShowIf("ShouldShowCustomMessageField")] [OnValueChanged("SetFromAttributes")] [SerializeField]
        private string customMessage;

        [OnValueChanged("SetFromAttributes")] [SerializeField]
        private Color tintOverride = Color.white;

        [OnValueChanged("SetFromAttributes")] [SerializeField]
        private float colorIntensityOverride = 1;

        [OnValueChanged("SetFromAttributes")] [SerializeField]
        private float scrollSpeedOverride;

        private BillboardData _billboardData;
        public Billboard Billboard => billboard;

        public BillboardData BillboardData {
            get => _billboardData;
            private set {
                _billboardData = value;
                billboardType = BillboardType.FromString(_billboardData.Name).Name;

                if (_billboardData is BillboardWithMessageData messageBillboard) billboard.CustomMessage = messageBillboard.Message;
                if (_billboardData is BillboardWithTextureData messageTexture) billboard.TextureResource = messageTexture.TextureResourceName;

                billboard.Tint = _billboardData.Tint;
                billboard.ColorIntensity = _billboardData.ColorIntensity;
                billboard.ScrollSpeed = _billboardData.ScrollSpeed;
            }
        }

        public void Deserialize(SerializableBillboard data) {
            var billboardType = BillboardType.FromString(data.type);
            BillboardData = billboardType.BillboardData;

            var billboardTransform = transform;
            billboardTransform.position = data.position.ToVector3();
            billboardTransform.rotation = Quaternion.Euler(data.rotation.ToVector3());

            // overrides
            if (!string.IsNullOrEmpty(data.customMessage)) customMessage = data.customMessage;
            if (data.tintOverride != null) tintOverride = data.tintOverride.ToColor();
            if (data.tintIntensityOverride != null) colorIntensityOverride = data.tintIntensityOverride.Value;
            if (data.scrollSpeedOverride != null) scrollSpeedOverride = data.scrollSpeedOverride.Value;

            BillboardData = billboardType.BillboardData;
        }

        private void OnEnable() {
            billboard = GetComponent<Billboard>();
        }

        [UsedImplicitly]
        private List<string> GetBillboardTypes() {
            return BillboardType.List().Select(b => b.Name).ToList();
        }

        [UsedImplicitly]
        private bool ShouldShowCustomMessageField() {
            return BillboardType.FromString(billboardType).BillboardData is BillboardWithMessageData;
        }

        [UsedImplicitly]
        private void RefreshFromBillboardData() {
            BillboardData = BillboardType.FromString(billboardType).BillboardData;
            ResetAll();
        }

        [UsedImplicitly]
        private void SetFromAttributes() {
            billboard.CustomMessage = customMessage != "" && ShouldShowCustomMessageField() ? customMessage : "";
            billboard.Tint = tintOverride;
            billboard.ColorIntensity = colorIntensityOverride;
            billboard.ScrollSpeed = scrollSpeedOverride;
        }

        [Button("Reset to type default")]
        private void ResetAll() {
            var billboardData = BillboardType.FromString(billboardType).BillboardData;
            customMessage = billboardData.Message;
            tintOverride = billboardData.Tint;
            colorIntensityOverride = billboardData.ColorIntensity;
            scrollSpeedOverride = billboardData.ScrollSpeed;
            SetFromAttributes();
        }
    }
}