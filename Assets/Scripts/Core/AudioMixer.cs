using Core;
using Misc;
using UnityEngine;
using UnityEngine.Audio;

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
        var master = MathfExtensions.Remap(0, 100, 0.0001f, 1, Preferences.Instance.GetFloat("volumeMaster"));
        var music = MathfExtensions.Remap(0, 100, 0.0001f, 1, Preferences.Instance.GetFloat("volumeMusic"));
        var sound = MathfExtensions.Remap(0, 100, 0.0001f, 1, Preferences.Instance.GetFloat("volumeSound"));
        var ui = MathfExtensions.Remap(0, 100, 0.0001f, 1, Preferences.Instance.GetFloat("volumeUi"));

        masterMixerGroup.audioMixer.SetFloat("masterVolume", Mathf.Log10(master) * 20);
        masterMixerGroup.audioMixer.SetFloat("musicVolume", Mathf.Log10(music) * 20);
        masterMixerGroup.audioMixer.SetFloat("soundVolume", Mathf.Log10(sound) * 20);
        masterMixerGroup.audioMixer.SetFloat("uiVolume", Mathf.Log10(ui) * 20);
    }
}