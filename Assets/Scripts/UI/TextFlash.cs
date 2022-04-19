using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TextFlash : MonoBehaviour {
    [SerializeField] private Color color1 = Color.black;
    [SerializeField] private Color color2 = Color.white;

    [Label("Interval (Seconds)")] [SerializeField]
    private float intervalSeconds;

    private readonly int _direction = 1;

    private Text _text;
    private float _timeCurrent;

    private void Update() {
        _text.color = Color.Lerp(color1, color2, _timeCurrent / intervalSeconds);
        _timeCurrent += Time.fixedUnscaledDeltaTime * _direction;
        _timeCurrent = Mathf.Sin(2 * Mathf.PI * (1 / intervalSeconds) * Time.time);
    }

    private void OnEnable() {
        _text = GetComponent<Text>();
    }
}