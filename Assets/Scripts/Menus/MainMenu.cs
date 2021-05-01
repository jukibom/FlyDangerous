using System.Collections;
using System.Collections.Generic;
using Audio;
using Menus.Options;
using UnityEngine;
using UnityEngine.UI;

namespace Menus {
    public class MainMenu : MonoBehaviour {

        [SerializeField]
        private TopMenu topMenu;
        
        [SerializeField]
        private OptionsMenu optionsMenu;

        [SerializeField] private Animator crossfade;
        [SerializeField] private Transform shipMeshTransform;
        [SerializeField] private GameObject alphaMessage;
        
        private Transform _transform;

        // Start is called before the first frame update
        void Awake() {
            StartCoroutine(ShowAlphaMessage());
            _transform = transform;
        }

        // Update is called once per frame
        void FixedUpdate() {
            // move along at a fixed rate to animate the stars
            // dirty hack job but who cares it's a menu screen
            _transform.Translate(0.1f, 0, 0.5f);

            // gently rock the ship mesh back and forth
            var rotationAmount = (0.25f - Mathf.PingPong(Time.time / 20, 0.5f)) / 5;
            shipMeshTransform.Rotate(Vector3.forward, rotationAmount);

        }

        public void Race() {
            AudioManager.Instance.Play("ui-confirm");
            StartCoroutine(LoadScene());
        }

        public void Freeplay() {
            AudioManager.Instance.Play("ui-confirm");
            StartCoroutine(LoadScene());
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
        
        IEnumerator ShowAlphaMessage() {
            
            // if it's disabled in the editor don't show this fade animation
            if (alphaMessage.activeSelf) {
                yield return new WaitForSeconds(6);
                crossfade.SetTrigger("FadeToBlack");
                yield return new WaitForSeconds(1);
                alphaMessage.SetActive(false);
                crossfade.SetTrigger("FadeFromBlack");
            }
        }

        IEnumerator LoadScene() {
            crossfade.SetTrigger("FadeToBlack");
            yield return new WaitForSeconds(1);
            Debug.Log("You should probably get off your ass and figure out scene loading now you tit");
        }
    }
}
