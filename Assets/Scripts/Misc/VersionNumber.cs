using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class VersionNumber : MonoBehaviour {
    public void Awake() {
        GetComponent<Text>().text = $"Fly Dangerous {Application.version}";
    }
}