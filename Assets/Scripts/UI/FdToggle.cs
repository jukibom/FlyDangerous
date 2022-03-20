using Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI {
    [RequireComponent(typeof(Toggle))]
    public class FdToggle : MonoBehaviour {
        [SerializeField] private string value;
        [SerializeField] public UnityEvent<string> onToggle;
        public string Value => value;

        public void OnToggle() {
            UIAudioManager.Instance.Play("ui-confirm");
            onToggle?.Invoke(value);
        }
    }
}