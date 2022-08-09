using System;
using System.Collections;
using UnityEngine;

namespace Bhaptics.Tact.Unity
{
    public class HapticSource : MonoBehaviour
    {
        public HapticClip clip;
        public bool playOnAwake = false;
        public bool loop = false;
        public float loopDelaySeconds = 0f;


        private Coroutine currentCoroutine, loopCoroutine;
        private bool isLooping = false;






        void Awake()
        {
            BhapticsManager.GetHaptic();

            if (Bhaptics_Setup.instance == null)
            {
                var findObjectOfType = FindObjectOfType<Bhaptics_Setup>();
                if (findObjectOfType == null)
                {
                    var go = new GameObject("[bhaptics]");
                    go.SetActive(false);
                    var setup = go.AddComponent<Bhaptics_Setup>();
                    var config = Resources.Load<BhapticsConfig>("BhapticsConfig");
                    if (config == null)
                    {
                        BhapticsLogger.LogError("Cannot find 'BhapticsConfig' in the Resources folder.");
                    }
                    setup.Config = config;
                    go.SetActive(true);
                }
            }
        }

        void OnEnable()
        {
            if (playOnAwake)
            {
                if (loop)
                {
                    PlayLoop();
                }
                else
                {
                    PlayHapticClip();
                }
            }
        }

        void OnDisable()
        {
            Stop();
        }







        public void Play()
        {
            PlayHapticClip();
        }

        public void PlayLoop()
        {
            if (clip == null)
            {
                BhapticsLogger.LogInfo("clip is null.");
                return;
            }

            isLooping = true;

            loopCoroutine = StartCoroutine(PlayLoopCoroutine());
        }

        public void PlayDelayed(float delaySecond = 0)
        {
            if (clip == null)
            {
                BhapticsLogger.LogInfo("clip is null.");
                return;
            }

            currentCoroutine = StartCoroutine(PlayCoroutine(delaySecond));
        }

        public void Stop()
        {
            if (loopCoroutine != null)
            {
                isLooping = false;
                StopCoroutine(loopCoroutine);
                loopCoroutine = null;
            }

            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
                currentCoroutine = null;
            }

            if (clip == null)
            {
                return;
            }

            clip.Stop();
        }

        public bool IsPlaying()
        {
            if (clip == null)
            {
                return false;
            }

            return clip.IsPlaying();
        }








        private IEnumerator PlayCoroutine(float delaySecond)
        {
            yield return new WaitForSeconds(delaySecond);

            PlayHapticClip();
            yield return null;
        }

        private void PlayHapticClip()
        {
            if (clip == null)
            {
                BhapticsLogger.LogInfo("clip is null");
                return;
            }

            clip.Play();
        }

        private IEnumerator PlayLoopCoroutine()
        {
            float clipDuration = Time.deltaTime;

            if (clip is FileHapticClip)
            {
                clipDuration = (clip as FileHapticClip).ClipDurationTime;
            }
            else if (clip is SimpleHapticClip)
            {
                clipDuration = (clip as SimpleHapticClip).TimeMillis;
            }

            WaitForSeconds duration = new WaitForSeconds(clipDuration * 0.001f * 0.95f);
            while (isLooping)
            {
                yield return new WaitForSeconds(loopDelaySeconds);
                PlayHapticClip();
                yield return duration;
            }
        }
    }
}
