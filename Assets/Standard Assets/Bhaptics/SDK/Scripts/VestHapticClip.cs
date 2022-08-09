using UnityEngine;

namespace Bhaptics.Tact.Unity
{
    public class VestHapticClip : FileHapticClip
    {
        [SerializeField, Range(0f, 360f)] protected float TactFileAngleX;
        [SerializeField, Range(-0.5f, 0.5f)] protected float TactFileOffsetY;

        private RotationOption _rotationOption = new RotationOption(0, 0);
        private ScaleOption _scaleOption = new ScaleOption(1, 1);


        #region Play method
        public override void Play()
        {
            Play(Intensity, Duration, 0f, 0f, "");
        }

        public override void Play(string identifier)
        {
            Play(Intensity, Duration, 0f, 0f, identifier);
        }

        public override void Play(float intensity, string identifier = "")
        {
            Play(intensity, Duration, 0f, 0f, identifier);
        }

        public override void Play(float intensity, float duration, string identifier = "")
        {
            Play(intensity, duration, 0f, 0f, identifier);
        }

        public override void Play(float intensity, float duration, float vestRotationAngleX, string identifier = "")
        {
            Play(intensity, duration, vestRotationAngleX, 0f, identifier);
        }

        public override void Play(float intensity, float duration, float vestRotationAngleX, float vestRotationOffsetY, string identifier = "")
        {
            if (!BhapticsManager.Init)
            {
                BhapticsManager.Initialize();
            }

            var haptic = BhapticsManager.GetHaptic();

            if (haptic == null)
            {
                return;
            }

            if (!haptic.IsFeedbackRegistered(assetId))
            {
                haptic.RegisterTactFileStr(assetId, JsonValue);
            }

            _rotationOption.OffsetAngleX = vestRotationAngleX + this.TactFileAngleX;
            _rotationOption.OffsetY = vestRotationOffsetY + this.TactFileOffsetY;

            _scaleOption.Intensity = intensity;
            _scaleOption.Duration = duration;

            haptic.SubmitRegistered(assetId, keyId + identifier,
                _rotationOption, _scaleOption);
        }
        #endregion

        public override void ResetValues()
        {
            base.ResetValues();
            this.TactFileAngleX = 0f;
            this.TactFileOffsetY = 0f;
        }
    }
}
