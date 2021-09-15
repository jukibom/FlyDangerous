using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio {
    public class UIAudioManager : MonoBehaviour {

        public static UIAudioManager Instance;
        public Sound[] sounds;

        void Awake() {
            // singleton shenanigans
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
                return;
            }

            foreach (Sound s in sounds) {
                s.source = gameObject.AddComponent<AudioSource>();
                s.source.playOnAwake = false;
                s.source.clip = s.clip;
                s.source.volume = s.volume;
                s.source.pitch = s.pitch;
                s.source.loop = s.loop;
            }
        }

        private void OnDestroy() {
            sounds = new Sound[] {};
        }

        public void Play(string name) {
            Sound sound = Array.Find(sounds, s => s.name == name);
            if (sound != null) {
                if (sound.source != null) {
                    sound.source.Play();
                }
                else {
                    Debug.LogWarning("Attempted to play missing sound " + name);
                }
            }
        }

        public void Stop(string name) {
            Sound sound = Array.Find(sounds, s => s.name == name);
            if (sound != null) {
                if (sound.source != null) {
                    sound.source.Stop();
                }
                else {
                    Debug.LogWarning("Attempted to stop missing sound " + name);
                }
            }
        }
    }
}