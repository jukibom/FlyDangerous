using System;
using System.Collections;
using UnityEngine;

namespace Menus.Main_Menu {
    [RequireComponent(typeof(AudioSource))]
    public class MainMenuMusic : MonoBehaviour {
        [SerializeField] private AudioClip musicIntro;

        public void PlayMenuMusic(bool includeIntro) {
            var source = GetComponent<AudioSource>();
            if (includeIntro) {
                source.PlayOneShot(musicIntro);
                source.PlayScheduled(AudioSettings.dspTime + musicIntro.length);
            }
            else {
                source.Play();
            }
        }
    }
}
