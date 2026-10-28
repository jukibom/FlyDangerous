using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace FdUI {
    [RequireComponent(typeof(TMPro.TMP_Text))]
    public class TextFlash : MonoBehaviour {
        [SerializeField] private Color color1 = Color.black;
        [SerializeField] private Color color2 = Color.white;

        [Label("Interval (Seconds)")] [SerializeField]
        private float intervalSeconds;

        private readonly int _direction = 1;

        private TMPro.TMP_Text _text;
        private float _timeCurrent;

        private void Update() {
            _text.color = Color.Lerp(color1, color2, _timeCurrent / intervalSeconds);
            _timeCurrent += Time.fixedUnscaledDeltaTime * _direction;
            _timeCurrent = Mathf.Sin(2 * Mathf.PI * (1 / intervalSeconds) * Time.time);
        }

        private void OnEnable() {
            _text = GetComponent<TMPro.TMP_Text>();
        }
    }
}