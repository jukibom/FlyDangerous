using System;
using Misc;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio {
    public class UIAudioManager : Singleton<UIAudioManager> {
        [SerializeField] private Sound[] sounds = { };
        [SerializeField] private AudioMixerGroup audioMixer;

        protected override void Awake() {
            base.Awake();
            foreach (var s in sounds) {
                s.source = gameObject.AddComponent<AudioSource>();
                s.source.outputAudioMixerGroup = audioMixer;
                s.source.playOnAwake = false;
                s.source.clip = s.clip;
                s.source.volume = s.volume;
                s.source.pitch = s.pitch;
                s.source.loop = s.loop;
            }
        }

        private void OnDestroy() {
            sounds = new Sound[] { };
        }

        public void Play(string name) {
            var sound = Array.Find(sounds, s => s.name == name);
            if (sound != null) {
                if (sound.source != null)
                    sound.source.Play();
                else
                    Debug.LogWarning("Attempted to play missing sound " + name);
            }
        }

        public void Stop(string name) {
            var sound = Array.Find(sounds, s => s.name == name);
            if (sound != null) {
                if (sound.source != null)
                    sound.source.Stop();
                else
                    Debug.LogWarning("Attempted to stop missing sound " + name);
            }
        }
    }
}