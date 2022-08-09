using UnityEngine;

namespace Bhaptics.Tact.Unity
{
    public class HapticReceiver : MonoBehaviour
    {
        public bool IsActive = true;
        public PositionTag PositionTag = PositionTag.Body;

        void Awake()
        {
            var col = GetComponent<Collider>();

            if (col == null)
            {
                BhapticsLogger.LogInfo("collider is not detected");
            }
        }

        void OnTriggerEnter(Collider bullet)
        {
            if (IsActive)
            {
                Handle(bullet.transform.position, bullet.GetComponent<HapticSender>());
            }
        }

        void OnCollisionEnter(Collision bullet)
        {
            if (IsActive)
            {
                Handle(bullet.contacts[0].point, bullet.gameObject.GetComponent<HapticSender>());
            }
        }

        private void Handle(Vector3 contactPoint, HapticSender tactSender)
        {
            if (tactSender != null)
            {
                var targetCollider = GetComponent<Collider>();
                tactSender.Play(PositionTag, contactPoint, targetCollider);
            }
        }
    }
}
