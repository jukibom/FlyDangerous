using System.Collections;
using UnityEngine;

namespace Menus.Main_Menu {
    [RequireComponent(typeof(AudioSource))]
    public class MainMenuMusic : MonoBehaviour {
        [SerializeField] private AudioClip musicIntro;
        private float _volume;

        private void Start() {
            var audioSource = GetComponent<AudioSource>();
            _volume = audioSource.volume;
        }

        public void PlayMenuMusic(bool includeIntro) {
            var audioSource = GetComponent<AudioSource>();
            if (includeIntro) {
                audioSource.PlayOneShot(musicIntro);
                audioSource.PlayScheduled(AudioSettings.dspTime + musicIntro.length);
            }
            else {
                audioSource.Play();
            }
        }

        public void FadeOut(float duration = 1) {
            var audioSource = GetComponent<AudioSource>();
            audioSource.volume = _volume;
            StartCoroutine(StartFade(audioSource, duration, 0));
        }

        public void FadeIn(float duration = 1) {
            var audioSource = GetComponent<AudioSource>();
            audioSource.volume = 0;
            StartCoroutine(StartFade(audioSource, duration, _volume));
        }

        private static IEnumerator StartFade(AudioSource audioSource, float duration, float targetVolume) {
            float currentTime = 0;
            var start = audioSource.volume;
            while (currentTime < duration) {
                currentTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(start, targetVolume, currentTime / duration);
                yield return null;
            }
        }
    }
}