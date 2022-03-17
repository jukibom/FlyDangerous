using Audio;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    [RequireComponent(typeof(Toggle))]
    public class FdToggle : MonoBehaviour {
        [SerializeField] private string value;
        public string Value => value;
        
        public void OnToggle() {
            UIAudioManager.Instance.Play("ui-confirm");
        }
    }
}
