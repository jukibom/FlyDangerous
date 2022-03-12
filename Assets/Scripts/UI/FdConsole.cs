using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class FdConsole : Singleton<FdConsole> {
        // TODO: make this log entry an array and trim as we go
        [SerializeField] private Text logEntry;

        public bool Visible { get; private set; }

        // Update is called once per frame
        private void Update() {
            var t = transform;
            if (Visible) {
                if (t.localScale.y < 1)
                    t.localScale = new Vector3(
                        1,
                        t.localScale.y + 0.05f,
                        1
                    );
                else
                    transform.localScale = Vector3.one;
            }
            else if (!Visible && transform.localScale.y > 0) {
                t.localScale = new Vector3(
                    1,
                    t.localScale.y - 0.05f,
                    1
                );
            }
        }

        public void Show() {
            Visible = true;
        }

        public void Hide() {
            Visible = false;
        }

        public void LogMessage(string message) {
            logEntry.text = logEntry.text + "\n" + message;
            Debug.Log(message);
        }

        public void Clear() {
            logEntry.text = "";
        }
    }
}