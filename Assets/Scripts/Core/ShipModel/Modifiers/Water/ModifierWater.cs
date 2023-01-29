using Core.Player;
using JetBrains.Annotations;
using MapMagic.Core;
using UnityEngine;
using UnityEngine.VFX;

namespace Core.ShipModel.Modifiers.Water {
    public enum WaterTransition {
        EnteringWater,
        LeavingWater
    }

    public class ModifierWater : MonoBehaviour, IModifier {
        [CanBeNull] public static ModifierWater Instance;

        [SerializeField] private float appliedDrag;
        [SerializeField] private float appliedAngularDrag;
        [SerializeField] private VisualEffect waterSubmergeVfx;
        [SerializeField] private VisualEffect waterEmergeVfx;

        [Tooltip("The default unity plane has a uv scale of 10 as it has 10x10 segments which each map to 1 full texture")] [SerializeField]
        private Vector2 planeUvScale = new(10, 10);

        private MeshRenderer _meshRenderer;

        private static readonly int FloatingOriginOffset = Shader.PropertyToID("_FloatingOriginOffset");
        private static readonly int PlaneSizeMeters = Shader.PropertyToID("_PlaneSizeMeters");
        private static readonly int WaterFadeDistantStart = Shader.PropertyToID("_WaterFadeDistantStart");
        private static readonly int WaterFadeDistantEnd = Shader.PropertyToID("_WaterFadeDistantEnd");

        public float WorldWaterHeight => FloatingOrigin.Instance.Origin.y + transform.position.y;

        private void OnEnable() {
            if (Instance != null) Debug.LogError("Multiple water modifiers in the scene! This is not allowed, expect nightmares");
            Instance = this;
            Game.OnGameSettingsApplied += OnGameSettingsApplied;
            FloatingOrigin.OnFloatingOriginCorrection += OnFloatingOriginCorrection;
            _meshRenderer = GetComponent<MeshRenderer>();

            var scale = transform.localScale;
            _meshRenderer.material.SetVector(PlaneSizeMeters, new Vector4(scale.x * planeUvScale.x, scale.z * planeUvScale.x));
        }

        private void OnDisable() {
            Instance = null;
            Game.OnGameSettingsApplied -= OnGameSettingsApplied;
            FloatingOrigin.OnFloatingOriginCorrection -= OnFloatingOriginCorrection;
        }

        private void FixedUpdate() {
            if (Game.Instance.GameModeHandler != null && Game.Instance.GameModeHandler.HasStarted) {
                var player = FdPlayer.FindLocalShipPlayer;
                if (player != null) {
                    var isUnderwaterNow = player.Position.y < transform.position.y;
                    if (isUnderwaterNow != Game.IsUnderWater) {
                        Game.Instance.WaterTransitioned(isUnderwaterNow);
                        player.ShipPhysics.WaterTransition(isUnderwaterNow ? WaterTransition.EnteringWater : WaterTransition.LeavingWater);
                    }
                }
            }
        }


        private void OnGameSettingsApplied() {
            var mapMagic = FindObjectOfType<MapMagicObject>();
            if (mapMagic) {
                var tileChunkCount = Preferences.Instance.GetFloat("graphics-terrain-chunks") + 1; // include drafts
                var tileSize = mapMagic.tileSize.x;
                var tileGenBuffer = 1.7f; // a little leeway for time to generate tiles in the distance

                var fogEndDistance = (tileSize * tileChunkCount - tileSize / 2) / tileGenBuffer;
                var fogStartDistance = Mathf.Max(1000f, fogEndDistance - fogEndDistance / 1.25f);

                _meshRenderer.material.SetFloat(WaterFadeDistantStart, fogStartDistance);
                _meshRenderer.material.SetFloat(WaterFadeDistantEnd, fogEndDistance);
            }
        }

        private void OnFloatingOriginCorrection(Vector3 offset) {
            var waterTransform = transform.parent;
            var position = waterTransform.position;

            // water stays at fixed x and z position but height is maintained by floating origin
            waterTransform.position = new Vector3(
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

        public void PlaySubmergedVfx(Vector3 atWorldPosition, Vector3 surfaceImpactVelocity) {
            var localPosition = transform.position + (atWorldPosition - FloatingOrigin.Instance.Origin);
            var localPlanePosition = new Vector3(localPosition.x, 0, localPosition.z);
            waterSubmergeVfx.SetVector3("_impactPositionLocal", localPlanePosition);
            waterSubmergeVfx.SetVector3("_impactVelocity", surfaceImpactVelocity);
            waterSubmergeVfx.Play();
        }

        public void PlayEmergedVfx(Vector3 atWorldPosition, Vector3 surfaceImpactVelocity) {
            var localPosition = transform.position + (atWorldPosition - FloatingOrigin.Instance.Origin);
            var localPlanePosition = new Vector3(localPosition.x, 0, localPosition.z);
            waterEmergeVfx.SetVector3("_impactPositionLocal", localPlanePosition);
            waterEmergeVfx.SetVector3("_impactVelocity", surfaceImpactVelocity);
            waterEmergeVfx.Play();
        }
    }
}