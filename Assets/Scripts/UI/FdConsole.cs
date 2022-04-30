using System.Collections;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class FdConsole : Singleton<FdConsole> {
        [SerializeField] private ScrollRect logEntryScrollRect;

        // TODO: Make this a trimmed array or something, no idea how slow this is!
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

            // need to wait one frame for the scroll rect to be ready to move
            IEnumerator ScrollToBottom() {
                yield return new WaitForEndOfFrame();
                logEntryScrollRect.verticalNormalizedPosition = 0f;
            }

            StartCoroutine(ScrollToBottom());
            Debug.Log(message);
        }

        public void Clear() {
            logEntry.text = "";
        }
    }
}