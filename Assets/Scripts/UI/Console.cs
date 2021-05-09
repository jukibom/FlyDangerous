using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Console : MonoBehaviour {

    public static Console Instance;

    [SerializeField] private Text _logEntry;
    
    private bool _show;
    public bool Visible { get => _show; }

    void Awake() {
        // singleton shenanigans
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update() {
        var t = transform;
        if (_show) {
            if (t.localScale.y < 1) {
                t.localScale = new Vector3(
                    1,
                    t.localScale.y + 0.05f,
                    1
                );
            }
            else {
                transform.localScale = Vector3.one;
            }
        }
        else if (!_show && transform.localScale.y > 0 ) {               
            t.localScale = new Vector3(
                1,
                t.localScale.y - 0.05f,
                1
            );
        }
    }

    public void Show() {
        _show = true;
    }

    public void Hide() {
        _show = false;
    }

    public void LogMessage(string message) {
        _logEntry.text = _logEntry.text + "\n" + message;
        Debug.Log(message);
    }
}
