using UnityEngine;
using UnityEngine.UI;

namespace Misc {
    [RequireComponent(typeof(TMPro.TMP_Text))]
    public class VersionNumber : MonoBehaviour {
        public void Awake() {
            GetComponent<TMPro.TMP_Text>().text =
                $"Fly Dangerous {Application.version} {SystemInfo.graphicsDeviceType}";
        }
    }
}