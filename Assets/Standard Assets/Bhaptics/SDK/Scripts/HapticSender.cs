using UnityEngine;

namespace Bhaptics.Tact.Unity
{
    public class HapticSender : MonoBehaviour
    {
        #region Haptic Clips
        public HapticClip[] DefaultClips;

        public HeadHapticClip[] HeadClips;
        public VestHapticClip[] BodyClips;
        public ArmsHapticClip[] LeftArmClips;
        public ArmsHapticClip[] RightArmClips;
        #endregion

        public float yOffsetMultiplier = 1f;


        public void Play(PositionTag posTag = PositionTag.Default)
        {
            Play(posTag, 0, 0);
        }

        public void Play(PositionTag posTag, Vector3 contactPos, Collider targetCollider)
        {
            Play(posTag, contactPos, targetCollider.transform.position, targetCollider.transform.forward, targetCollider.bounds.size.y);
        }

        private void Play(PositionTag posTag, Vector3 contactPos, Vector3 targetPos, Vector3 targetForward, float targetHeight)
        {
            var angle = 0f;
            var offsetY = 0f;

            if (posTag == PositionTag.Body)
            {
                Vector3 targetDir = contactPos - targetPos;
                angle = BhapticsUtils.Angle(targetDir, targetForward);
                offsetY = (contactPos.y - targetPos.y) / targetHeight;
            }

            Play(posTag, angle, offsetY);
        }

        public void Play(PositionTag posTag, RaycastHit hit)
        {
            var col = hit.collider;
            Play(posTag, hit.point, col.transform.position, col.transform.forward, col.bounds.size.y);
        }

        private HapticClip GetClip(PositionTag posTag)
        {
            switch (posTag)
            {
                case PositionTag.Body:
                    if (BodyClips != null && BodyClips.Length > 0)
                    {
                        int randIndex = Random.Range(0, BodyClips.Length);
                        return BodyClips[randIndex];
                    }
                    break;
                case PositionTag.Head:
                    if (HeadClips != null && HeadClips.Length > 0)
                    {
                        int randIndex = Random.Range(0, HeadClips.Length);
                        return HeadClips[randIndex];
                    }
                    break;
                case PositionTag.RightArm:
                    if (RightArmClips != null && RightArmClips.Length > 0)
                    {
                        int randIndex = Random.Range(0, RightArmClips.Length);
                        return  RightArmClips[randIndex];
                    }
                    break;
                case PositionTag.LeftArm:
                    if (LeftArmClips != null && LeftArmClips.Length > 0)
                    {
                        int randIndex = Random.Range(0, LeftArmClips.Length);
                        return LeftArmClips[randIndex];
                    }
                    break;
            }

            if (DefaultClips != null && DefaultClips.Length > 0)
            {
                int randIndex = Random.Range(0, DefaultClips.Length);
                return DefaultClips[randIndex];
            }

            return null;
        }

        public bool IsPlaying()
        {
            return false;
        }

        public void Play(PositionTag posTag, float angleX, float offsetY)
        {
            var clip = GetClip(posTag);
            if (clip == null)
            {
                BhapticsLogger.LogInfo("Cannot find TactClip {0} {1} {2}", posTag, angleX, offsetY);
                return;
            }
            
            clip.Play(1f, 1f, angleX, offsetY * yOffsetMultiplier);
        }
    }

    public enum PositionTag
    {
        Body, Head, LeftArm, RightArm, Default
    }
}