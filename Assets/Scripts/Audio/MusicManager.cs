﻿using System;
using System.Collections;
using JetBrains.Annotations;
using Misc;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio {
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : Singleton<MusicManager> {
        [SerializeField] private AudioMixerGroup musicMixer;
        [SerializeField] private AudioMixerGroup mainMenuMusicMixer;

        private AudioSource _audioSource;
        private Coroutine _fadeCoroutine;

        [CanBeNull] private AudioClip _introTrack;
        private AudioClip _loopingTrack;
        private AudioMixerGroup _mixerGroupToUseOnPlay;
        private float _volume;
        [CanBeNull] public MusicTrack CurrentPlayingTrack { get; private set; }

        private void OnEnable() {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.priority = 0;
            _volume = _audioSource.volume;
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        private void OnDisable() {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        }

        public void PlayMusic(MusicTrack musicTrack, bool includeIntro, bool fadeOut, bool fadeIn, bool useMainMenuVolumeMixer = false) {
            _mixerGroupToUseOnPlay = useMainMenuVolumeMixer ? mainMenuMusicMixer : musicMixer;

            var introTrack = musicTrack.HasIntro ? Resources.Load<AudioClip>($"Music/{musicTrack.IntroTrackToLoad}") : null;
            var loopingTrack = Resources.Load<AudioClip>($"Music/{musicTrack.MusicTrackToLoad}");

            // if we're already playing this, do nothing! yay!
            if (_loopingTrack == loopingTrack && _audioSource.isPlaying)
                return;

            _introTrack = introTrack;
            _loopingTrack = loopingTrack;

            if (fadeOut)
                FadeOut(1f, () => Play(musicTrack, includeIntro));
            else Play(musicTrack, includeIntro);

            if (fadeIn) FadeIn();
        }

        public void StopMusic(bool fade = false) {
            CurrentPlayingTrack = null;
            if (fade)
                FadeOut(1f, () => _audioSource.Stop());
            else
                _audioSource.Stop();
        }

        private void Play([CanBeNull] MusicTrack musicTrack, bool includeIntro = true) {
            if (musicTrack == null) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            StopMusic();
            CurrentPlayingTrack = musicTrack;

            _audioSource.outputAudioMixerGroup = _mixerGroupToUseOnPlay;
            _audioSource.volume = _volume;
            _audioSource.clip = _loopingTrack;
            if (_introTrack && includeIntro) {
                _audioSource.PlayOneShot(_introTrack);
                _audioSource.PlayScheduled(AudioSettings.dspTime + _introTrack.length);
            }
            else {
                _audioSource.Play();
            }
        }

        private void FadeOut(float duration = 1, Action onComplete = null) {
            _audioSource.volume = _volume;
            _fadeCoroutine = StartCoroutine(StartFade(_audioSource, duration, 0, onComplete));
        }

        private void FadeIn(float duration = 1, Action onComplete = null) {
            _audioSource.volume = 0;
            _fadeCoroutine = StartCoroutine(StartFade(_audioSource, duration, _volume, onComplete));
        }

        private static IEnumerator StartFade(AudioSource audioSource, float duration, float targetVolume, Action onComplete = null) {
            float currentTime = 0;
            var start = audioSource.volume;
            while (currentTime < duration) {
                currentTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(start, targetVolume, currentTime / duration);
                yield return null;
            }

            onComplete?.Invoke();
        }

        // Music has to be manually restarted whenever audio config changes
        private void OnAudioConfigurationChanged(bool deviceWasChanged) {
            Play(CurrentPlayingTrack);
        }
    }
}