using NaughtyAttributes;
using UnityEngine;

namespace Gameplay {
    public enum CheckpointType {
        Start,
        Check,
        End
    }

    public class Checkpoint : MonoBehaviour {
        // Invoke with the checkpoint object and the amount of time, in ms, left to hit the collider within the frame
        public delegate void CheckpointHit(Checkpoint checkpoint, float excessTimeToHitSeconds);

        public event CheckpointHit OnHit;

        [OnValueChanged("Reset")] [SerializeField]
        private CheckpointType type = CheckpointType.Check;

        [SerializeField] private MeshRenderer overlay;
        [SerializeField] private Material checkMaterial;
        [SerializeField] private Material validEndMaterial;
        [SerializeField] private Material invalidEndMaterial;
        [SerializeField] private AudioSource checkpointAudioSource;

        private bool _isActive;
        private bool _isValidEnd;
        private static readonly int seed = Shader.PropertyToID("_Seed");

        public CheckpointType Type {
            get => type;
            set {
                type = value;
                Reset();
            }
        }

        public bool IsHit => !_isActive;

        public void Reset() {
            ShowOverlay();
            if (Type == CheckpointType.Start) HideOverlay();

            if (Type == CheckpointType.Check) overlay.material = checkMaterial;

            if (Type == CheckpointType.End) overlay.material = invalidEndMaterial;

            overlay.material.SetFloat(seed, Random.Range(0, 1f));

            _isValidEnd = false;
            _isActive = Type != CheckpointType.Start;
        }

        public void ToggleValidEndMaterial(bool isEnabled) {
            overlay.material = isEnabled ? validEndMaterial : invalidEndMaterial;
            _isValidEnd = isEnabled;
        }

        public void Hit(float excessTimeToHitSeconds) {
            if (Type == CheckpointType.End && !_isValidEnd) return;

            if (_isActive) {
                _isActive = false;
                OnHit?.Invoke(this, excessTimeToHitSeconds);
                checkpointAudioSource.Play();
                HideOverlay();
            }
        }

        private void ShowOverlay() {
            overlay.gameObject.SetActive(true);
        }

        private void HideOverlay() {
            overlay.gameObject.SetActive(false);
        }
    }
}