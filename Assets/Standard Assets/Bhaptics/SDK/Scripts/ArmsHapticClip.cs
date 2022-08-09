using UnityEngine;


namespace Bhaptics.Tact.Unity
{
    public class ArmsHapticClip : FileHapticClip
    {
        public bool IsReflect;



        public override void Play(float intensity, float duration, float vestRotationAngleX, float vestRotationOffsetY, string identifier = "")
        {
            if (!BhapticsManager.Init)
            {
                BhapticsManager.Initialize();
                //return;
            }

            var haptic = BhapticsManager.GetHaptic();

            if (haptic == null)
            {
                return;
            }

            if (IsReflect)
            {
                var reflectIdentifier = assetId + "Reflect";

                if (!haptic.IsFeedbackRegistered(reflectIdentifier))
                {
                    haptic.RegisterTactFileStrReflected(reflectIdentifier, JsonValue);
                }

                haptic.SubmitRegistered(reflectIdentifier, keyId + identifier, new ScaleOption(intensity, duration));
            }
            else
            {
                if (!haptic.IsFeedbackRegistered(assetId))
                {
                    haptic.RegisterTactFileStr(assetId, JsonValue);
                }

                haptic.SubmitRegistered(assetId, keyId + identifier, new ScaleOption(intensity, duration));
            }
        }

        public override void ResetValues()
        {
            base.ResetValues();
            IsReflect = false;
        }

    }
}