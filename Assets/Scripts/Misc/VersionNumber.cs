using UnityEngine;
using UnityEngine.UI;

namespace Misc {
    [RequireComponent(typeof(Text))]
    public class VersionNumber : MonoBehaviour {
        public void Awake() {
            GetComponent<Text>().text =
                $"Fly Dangerous {Application.version} {SystemInfo.graphicsDeviceType}";
        }
    }
}