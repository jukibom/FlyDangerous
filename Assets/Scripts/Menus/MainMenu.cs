using System.Collections;
using System.Collections.Generic;
using Audio;
using Menus.Options;
using UnityEngine;

namespace Menus {
    public class MainMenu : MonoBehaviour {

        [SerializeField]
        private TopMenu topMenu;
        
        [SerializeField]
        private OptionsMenu optionsMenu;
        
        private Transform _transform;

        // Start is called before the first frame update
        void Start() {
            _transform = transform;
        }

        // Update is called once per frame
        void FixedUpdate() {
            // move along at a fixed rate to animate the stars
            // dirty hack job but who cares it's a menu screen
            _transform.Translate(0.1f, 0, 0.5f);
        }

        public void Race() {
            AudioManager.Instance.Play("ui-confirm");
        }

        public void Freeplay() {
            AudioManager.Instance.Play("ui-confirm");
        }

        public void OpenOptionsPanel() {
            AudioManager.Instance.Play("ui-dialog-open");
            topMenu.Hide();
            optionsMenu.Show();
        }

        public void CloseOptionsPanel() {
            optionsMenu.Hide();
            topMenu.Show();
        }
        
        public void OpenDiscordLink() {
            AudioManager.Instance.Play("ui-dialog-open");
            Application.OpenURL("https://discord.gg/4daSEUKZ6A");
        }

        public void Quit() {
            Application.Quit();
            AudioManager.Instance.Play("ui-cancel");
        }
    }
}
