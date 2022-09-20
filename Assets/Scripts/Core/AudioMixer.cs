using Misc;
using UnityEngine;
using UnityEngine.Audio;

namespace Core {
    public class AudioMixer : Singleton<AudioMixer> {
        [SerializeField] private AudioMixerGroup masterMixerGroup;

        private void OnEnable() {
            Game.OnGameSettingsApplied += OnSettingsApplied;
            OnSettingsApplied();
        }

        private void OnDisable() {
            Game.OnGameSettingsApplied -= OnSettingsApplied;
        }

        private void OnSettingsApplied() {
            var audioConfiguration = AudioSettings.GetConfiguration();
            var currentSpeakerMode = audioConfiguration.speakerMode;
            switch (Preferences.Instance.GetString("audioMode")) {
                case "mono":
                    audioConfiguration.speakerMode = AudioSpeakerMode.Mono;
                    break;
                case "stereo":
                    audioConfiguration.speakerMode = AudioSpeakerMode.Stereo;
                    break;
                case "quad":
                    audioConfiguration.speakerMode = AudioSpeakerMode.Quad;
                    break;
                case "surround":
                    audioConfiguration.speakerMode = AudioSpeakerMode.Surround;
                    break;
                case "5.1":
                    audioConfiguration.speakerMode = AudioSpeakerMode.Mode5point1;
                    break;
                case "7.1":
                    audioConfiguration.speakerMode = AudioSpeakerMode.Mode7point1;
                    break;
                case "dolby dts":
                    audioConfiguration.speakerMode = AudioSpeakerMode.Prologic;
                    break;
            }

            if (audioConfiguration.speakerMode != currentSpeakerMode)
                AudioSettings.Reset(audioConfiguration);

            var master = Preferences.Instance.GetFloat("volumeMaster").Remap(0, 100, 0.0001f, 1);
            var music = Preferences.Instance.GetFloat("volumeMusic").Remap(0, 100, 0.0001f, 1);
            var sound = Preferences.Instance.GetFloat("volumeSound").Remap(0, 100, 0.0001f, 1);
            var ghostSound = Preferences.Instance.GetFloat("volumeGhostSound").Remap(0, 100, 0.0001f, 1);
            var ui = Preferences.Instance.GetFloat("volumeUi").Remap(0, 100, 0.0001f, 1);

            masterMixerGroup.audioMixer.SetFloat("masterVolume", Mathf.Log10(master) * 20);
            masterMixerGroup.audioMixer.SetFloat("musicVolume", Mathf.Log10(music) * 20);
            masterMixerGroup.audioMixer.SetFloat("soundVolume", Mathf.Log10(sound) * 20);
            masterMixerGroup.audioMixer.SetFloat("ghostSoundVolume", Mathf.Log10(ghostSound) * 20);
            masterMixerGroup.audioMixer.SetFloat("uiVolume", Mathf.Log10(ui) * 20);
        }

        public void SetMusicLowPassEnabled(bool isEnabled) {
            masterMixerGroup.audioMixer.SetFloat("musicCutoffFreq", isEnabled ? 480 : 22000);
        }
    }
}