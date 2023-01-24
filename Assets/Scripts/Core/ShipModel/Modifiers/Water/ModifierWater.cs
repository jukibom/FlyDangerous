using Core.Player;
using MapMagic.Core;
using UnityEngine;

namespace Core.ShipModel.Modifiers.Water {
    public class ModifierWater : MonoBehaviour, IModifier {
        [SerializeField] private float appliedDrag;
        [SerializeField] private float appliedAngularDrag;

        [Tooltip("The default unity plane has a uv scale of 10 as it has 10x10 segments which each map to 1 full texture")] [SerializeField]
        private Vector2 planeUvScale = new(10, 10);

        private MeshRenderer _meshRenderer;

        private static readonly int FloatingOriginOffset = Shader.PropertyToID("_FloatingOriginOffset");
        private static readonly int PlaneSizeMeters = Shader.PropertyToID("_PlaneSizeMeters");
        private static readonly int WaterFadeDistantStart = Shader.PropertyToID("_WaterFadeDistantStart");
        private static readonly int WaterFadeDistantEnd = Shader.PropertyToID("_WaterFadeDistantEnd");

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
            FloatingOrigin.OnFloatingOriginCorrection += OnFloatingOriginCorrection;
            _meshRenderer = GetComponent<MeshRenderer>();

            var scale = transform.localScale;
            _meshRenderer.material.SetVector(PlaneSizeMeters, new Vector4(scale.x * planeUvScale.x, scale.z * planeUvScale.x));
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
            FloatingOrigin.OnFloatingOriginCorrection -= OnFloatingOriginCorrection;
        }

        private void FixedUpdate() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player != null) {
                var isUnderwaterNow = player.Position.y < transform.position.y;
                if (isUnderwaterNow != Game.IsUnderWater) Game.Instance.WaterTransitioned(isUnderwaterNow);
            }
        }

        private void OnGameSettingsApplied() {
            var mapMagic = FindObjectOfType<MapMagicObject>();
            if (mapMagic) {
                var tileChunkCount = Preferences.Instance.GetFloat("graphics-terrain-chunks") + 1; // include drafts
                var tileSize = mapMagic.tileSize.x;

                var fogEndDistance = tileSize * tileChunkCount - tileSize / 2;
                var fogStartDistance = Mathf.Max(1000f, fogEndDistance - fogEndDistance / 1.5f);

                _meshRenderer.material.SetFloat(WaterFadeDistantStart, fogStartDistance);
                _meshRenderer.material.SetFloat(WaterFadeDistantEnd, fogEndDistance);
            }
        }

        private void OnFloatingOriginCorrection(Vector3 offset) {
            var planeTransform = transform;
            var position = planeTransform.position;

            // water stays at fixed x and z position but height is maintained by floating origin
            planeTransform.position = new Vector3(
                position.x,
                position.y - offset.y,
                position.z
            );

            var x = -FloatingOrigin.Instance.Origin.x;
            var y = -FloatingOrigin.Instance.Origin.z;
            _meshRenderer.material.SetVector(FloatingOriginOffset, new Vector4(x, y));
        }

        // when underwater
        public void ApplyModifierEffect(Rigidbody ship, ref AppliedEffects effects) {
            effects.shipDrag = appliedDrag;
            effects.shipAngularDrag = appliedAngularDrag;
        }
    }
}