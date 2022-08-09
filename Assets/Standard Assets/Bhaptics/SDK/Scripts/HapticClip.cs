using System;
using System.Collections;
using UnityEngine;

namespace Bhaptics.Tact.Unity
{
    public enum HapticDeviceType
    {
        None,
        Tactal, TactSuit, Tactosy_arms, Tactosy_hands, Tactosy_feet,
        TactGlove,
    }

    [Serializable]
    public enum HapticClipPositionType
    {
        VestFront,
        VestBack,
        Head,
        RightForearm,
        LeftForearm,
        LeftHand,
        RightHand,
        LeftFoot,
        RightFoot,
        LeftGlove,
        RightGlove
    }

    public class HapticClip : ScriptableObject
    {
        [NonSerialized] protected string assetId = System.Guid.NewGuid().ToString();

        [NonSerialized] public string keyId = System.Guid.NewGuid().ToString();




        #region Play method
        /// <summary>
        /// Play the haptic feedback.
        /// </summary>
        public virtual void Play()
        {
            Play(1f, 1f, 0f, 0f, "");
        }

        /// <summary>
        /// Play the haptic feedback with identifier.
        /// </summary>
        /// <param name="identifier">Use when playing haptic feedback independently.</param>
        public virtual void Play(string identifier)
        {
            Play(1f, 1f, 0f, 0f, identifier);
        }

        /// <summary>
        /// Play the haptic feedback with optional values.
        /// </summary>
        /// <param name="intensity">Intensity value of haptic feedback.(Overwrite, not multiplication)</param>
        /// <param name="identifier">Use when playing haptic feedback independently.</param>
        public virtual void Play(float intensity, string identifier = "")
        {
            Play(intensity, 1f, 0f, 0f, identifier);
        }

        /// <summary>
        /// Play the haptic feedback with optional values.
        /// </summary>
        /// <param name="intensity">Intensity value of haptic feedback.(Overwrite, not multiplication)</param>
        /// <param name="duration">Duration value of haptic feedback.(Overwrite, not multiplication)</param>
        /// <param name="identifier">Use when playing haptic feedback independently.</param>
        public virtual void Play(float intensity, float duration, string identifier = "")
        {
            Play(intensity, duration, 0f, 0f, identifier);
        }

        /// <summary>
        /// Play the haptic feedback with optional values.
        /// </summary>
        /// <param name="intensity">Intensity value of haptic feedback.(Overwrite, not multiplication)</param>
        /// <param name="duration">Duration value of haptic feedback.(Overwrite, not multiplication)</param>
        /// <param name="vestRotationAngleX">Rotation value relative to the y-axis.(* Applies to only VestHapticClip)</param>
        /// <param name="identifier">Use when playing haptic feedback independently.</param>
        public virtual void Play(float intensity, float duration, float vestRotationAngleX, string identifier = "")
        {
            Play(intensity, duration, vestRotationAngleX, 0f, identifier);
        }

        /// <summary>
        /// Play the haptic feedback with optional values.
        /// </summary>
        /// <param name="contactPos"></param>
        /// <param name="targetCollider"></param>
        /// <param name="identifier">Use when playing haptic feedback independently.</param>
        public virtual void Play(Vector3 contactPos, Collider targetCollider, string identifier = "")
        {
            Play(contactPos, targetCollider.bounds.center, targetCollider.transform.forward, targetCollider.bounds.size.y, identifier);
        }

        /// <summary>
        /// Play the haptic feedback with optional values.
        /// </summary>
        /// <param name="contactPos"></param>
        /// <param name="targetPos"></param>
        /// <param name="targetForward"></param>
        /// <param name="targetHeight"></param>
        /// <param name="identifier">Use when playing haptic feedback independently.</param>
        public virtual void Play(Vector3 contactPos, Vector3 targetPos, Vector3 targetForward, float targetHeight, string identifier = "")
        {
            Vector3 targetDir = contactPos - targetPos;

            var angle = BhapticsUtils.Angle(targetDir, targetForward);
            
            var offsetY = (contactPos.y - targetPos.y) / targetHeight;

            Play(1f, 1f, angle, offsetY, identifier);
        }

        /// <summary>
        /// Play the haptic feedback with optional values.
        /// </summary>
        /// <param name="intensity">Intensity value of haptic feedback.(Overwrite, not multiplication)</param>
        /// <param name="duration">Duration value of haptic feedback.(Overwrite, not multiplication)</param>
        /// <param name="contactPos"></param>
        /// <param name="targetPos"></param>
        /// <param name="targetForward"></param>
        /// <param name="targetHeight"></param>
        /// <param name="identifier">Use when playing haptic feedback independently.</param>
        public virtual void Play(float intensity, float duration, Vector3 contactPos, Vector3 targetPos, Vector3 targetForward, float targetHeight, string identifier = "")
        {
            Vector3 targetDir = contactPos - targetPos;

            var angle = BhapticsUtils.Angle(targetDir, targetForward);

            var offsetY = (contactPos.y - targetPos.y) / targetHeight;

            Play(intensity, duration, angle, offsetY, identifier);
        }

        /// <summary>
        /// Play the haptic feedback with optional values.
        /// </summary>
        /// <param name="intensity">Intensity value of haptic feedback.(Overwrite, not multiplication)</param>
        /// <param name="duration">Duration value of haptic feedback.(Overwrite, not multiplication)</param>
        /// <param name="vestRotationAngleX">Rotation value relative to the y-axis.(* Applies to only VestHapticClip)</param>
        /// <param name="vestRotationOffsetY">Height value based on vertical.(* Applies to only VestHapticClip)</param>
        /// <param name="identifier">Use when playing haptic feedback independently.</param>
        public virtual void Play(float intensity, float duration, float vestRotationAngleX, float vestRotationOffsetY, string identifier = "")
        {
        }
        #endregion







        public virtual void Stop()
        {
            var haptic = BhapticsManager.GetHaptic();

            if (haptic != null)
            {
                haptic.TurnOff();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier">Use when stopping haptic feedback independently.</param>
        public virtual void Stop(string identifier)
        {
            var haptic = BhapticsManager.GetHaptic();

            if (haptic != null)
            {
                haptic.TurnOff(keyId + identifier);
            }
        }

        public virtual bool IsPlaying()
        {
            var haptic = BhapticsManager.GetHaptic();

            if (haptic == null)
            {
                return false;
            }

            return haptic.IsPlaying(keyId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier">Use when checking haptic feedback independently.</param>
        /// <returns></returns>
        public virtual bool IsPlaying(string identifier)
        {
            var haptic = BhapticsManager.GetHaptic();

            if (haptic == null)
            {
                return false;
            }

            return haptic.IsPlaying(keyId + identifier);
        }

        public virtual void ResetValues()
        {

        }



        public string GetAssetID()
        {
            return assetId;
        }
    }
}
