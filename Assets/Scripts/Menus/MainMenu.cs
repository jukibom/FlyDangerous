using System.Collections;
using System.Collections.Generic;
using Audio;
using Menus.Options;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Menus {
    public class MainMenu : MonoBehaviour {

        [SerializeField]
        private TopMenu topMenu;
        
        [SerializeField]
        private OptionsMenu optionsMenu;

        [SerializeField] private GameObject shipMesh;
        [SerializeField] private GameObject alphaMessage;
        
        // Start is called before the first frame update
        void Awake() {
            StartCoroutine(ShowAlphaMessage());
        }

        // Update is called once per frame
        void FixedUpdate() {
            // move along at a fixed rate to animate the stars
            // dirty hack job but who cares it's a menu screen
            transform.Translate(0.1f, 0, 0.5f);

            // gently rock the ship mesh back and forth
            var rotationAmount = (0.25f - Mathf.PingPong(Time.time / 20, 0.5f)) / 5;
            shipMesh.transform.Rotate(Vector3.forward, rotationAmount);
        }

        public void Race() {
            AudioManager.Instance.Play("ui-confirm");
            Game.Instance.StartGame("MapTest");
            topMenu.Hide();
        }

        public void FreePlay() {
            AudioManager.Instance.Play("ui-confirm");
            Game.Instance.StartGame("Terrain");
            topMenu.Hide();
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
            Game.Instance.QuitGame();
            AudioManager.Instance.Play("ui-cancel");
        }

        IEnumerator ShowAlphaMessage() {
            // if it's disabled in the editor don't show this fade animation
            if (alphaMessage.activeSelf) {
                shipMesh.SetActive(false);
                yield return new WaitForSeconds(6);
                Game.Instance.FadeToBlack();
                yield return new WaitForSeconds(1);
                alphaMessage.SetActive(false);
                Game.Instance.FadeFromBlack();
                shipMesh.SetActive(true);
            }
        }
    }
}
