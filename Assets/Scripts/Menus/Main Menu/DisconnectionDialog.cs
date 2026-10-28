using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class DisconnectionDialog : MenuBase {
        [SerializeField] private TMPro.TMP_Text reasonText;

        public string Reason {
            get => reasonText.text;
            set => reasonText.text = value;
        }

        public void Close() {
            Cancel();
        }
    }
}