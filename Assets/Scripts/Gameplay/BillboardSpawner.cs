using System;
using System.Collections.Generic;
using System.Linq;
using Core.MapData;
using Core.MapData.Serializable;
using Core.ShipModel.Modifiers.Water;
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

        [ShowIf("ShouldShowCustomMessageField")] [OnValueChanged("SetBillboardAttributes")] [SerializeField]
        private string customMessage;

        [OnValueChanged("SetBillboardAttributes")] [SerializeField]
        private Color colorTint = Color.white;

        [OnValueChanged("SetBillboardAttributes")] [SerializeField]
        private float colorIntensity = 1;

        [OnValueChanged("SetBillboardAttributes")] [SerializeField]
        private float scrollSpeed;

        private BillboardData _billboardData;
        public Billboard Billboard => billboard;

        public BillboardData BillboardData {
            get {
                if (_billboardData == null) _billboardData = BillboardType.FromString(billboardType).BillboardData;
                return _billboardData;
            }
            private set {
                _billboardData = value;
                billboardType = BillboardType.FromString(_billboardData.Name).Name;

                // if we have water in the scene, use fallback dithering to avoid z-buffer bullshit (otherwise use glorious alpha)
                billboard.UseDithering = FindObjectOfType<ModifierWater>() != null;

                if (_billboardData is BillboardWithMessageData messageBillboard) billboard.CustomMessage = messageBillboard.Message;
                if (_billboardData is BillboardWithTextureData messageTexture) billboard.TextureResource = messageTexture.TextureResourceName;

                billboard.Tint = _billboardData.Tint;
                billboard.ColorIntensity = _billboardData.ColorIntensity;
                billboard.ScrollSpeed = _billboardData.ScrollSpeed;
            }
        }

        private void Awake() {
            RefreshFromBillboardData();
        }

        private void OnEnable() {
            billboard = GetComponent<Billboard>();
        }

        public void Deserialize(SerializableBillboard serializedData) {
            var serializedBillboardType = BillboardType.FromString(serializedData.type);
            BillboardData = serializedBillboardType.BillboardData;

            var billboardTransform = transform;
            billboardTransform.position = serializedData.position.ToVector3();
            billboardTransform.rotation = Quaternion.Euler(serializedData.rotation.ToVector3());

            // overrides
            if (!string.IsNullOrEmpty(serializedData.customMessage)) customMessage = serializedData.customMessage;

            colorTint = serializedData.tintOverride != null && !serializedData.tintOverride.ToColor().Equals(BillboardData.Tint)
                ? serializedData.tintOverride.ToColor()
                : BillboardData.Tint;

            colorIntensity =
                serializedData.tintIntensityOverride != null && Math.Abs(serializedData.tintIntensityOverride.Value - BillboardData.ColorIntensity) > 0.01f
                    ? serializedData.tintIntensityOverride.Value
                    : BillboardData.ColorIntensity;

            scrollSpeed =
                serializedData.scrollSpeedOverride != null && Math.Abs(serializedData.scrollSpeedOverride.Value - BillboardData.ScrollSpeed) > 0.01f
                    ? serializedData.scrollSpeedOverride.Value
                    : BillboardData.ScrollSpeed;

            SetBillboardAttributes();
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

        /**
         * Apply properties to billboard using overrides if different from the base data
         */
        [UsedImplicitly]
        private void SetBillboardAttributes() {
            billboard.CustomMessage = customMessage != "" && ShouldShowCustomMessageField() ? customMessage : "";
            billboard.Tint = colorTint;
            billboard.ColorIntensity = colorIntensity;
            billboard.ScrollSpeed = scrollSpeed;
        }

        [Button("Reset to type default")]
        private void ResetAll() {
            var billboardData = BillboardType.FromString(billboardType).BillboardData;
            customMessage = billboardData.Message;
            colorTint = billboardData.Tint;
            colorIntensity = billboardData.ColorIntensity;
            scrollSpeed = billboardData.ScrollSpeed;
            SetBillboardAttributes();
        }
    }
}