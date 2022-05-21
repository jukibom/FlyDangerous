using System;
using System.Collections;
using JetBrains.Annotations;
using Misc;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

namespace GameUI.GameModes {
    public class MedalsScreen : MonoBehaviour {
        [SerializeField] private VisualEffect dustImpactEffect;
        [SerializeField] private GameObject newPersonalBest;
        [SerializeField] private GameObject resultNotValid;
        [SerializeField] private GameObject noMedalAwarded;
        [SerializeField] private GameObject bronzeMedal;
        [SerializeField] private GameObject silverMedal;
        [SerializeField] private GameObject goldMedal;
        [SerializeField] private GameObject authorMedal;
        [SerializeField] private Text resultDeltaText;
        [SerializeField] private Text resultText;
        [SerializeField] private AudioSource medalDingAudio;
        [SerializeField] private AudioSource medalThudAudio;
        [SerializeField] private AudioSource medalAuthorAudio;
        [SerializeField] private AudioSource scoreCheerAudio;

        [Label("Duration of each medal animation (seconds)")] [SerializeField]
        private float animationDuration = 0.1f;

        [Label("Duration interval between medals (seconds)")] [SerializeField]
        private float animationInterval = 0.2f;


        private void OnEnable() {
            ClearMedalScreen();
            resultText.text = "";
            resultDeltaText.text = "";
        }

        private void ClearMedalScreen() {
            noMedalAwarded.SetActive(false);
            bronzeMedal.SetActive(false);
            silverMedal.SetActive(false);
            goldMedal.SetActive(false);
            authorMedal.SetActive(false);
            newPersonalBest.SetActive(false);
            resultNotValid.SetActive(false);
        }

        [Button("Test Animation")]
        [UsedImplicitly]
        private void TestAnimation() {
            StartCoroutine(ShowAnimation(4, true, 32.23f, 30, true));
        }

        public IEnumerator ShowAnimation(uint medalCount, bool personalBest, float result, float previousResult, bool isValid) {
            ClearMedalScreen();

            if (!isValid) {
                personalBest = false;
                medalCount = 0;
            }

            resultText.text = TimeExtensions.TimeSecondsToString(result);

            noMedalAwarded.SetActive(medalCount == 0);

            switch (medalCount) {
                case 4:
                    yield return AnimateMedal(bronzeMedal, animationInterval, () => PlayMedalDing(0.7f));
                    yield return AnimateMedal(silverMedal, animationInterval * 1.5f, () => PlayMedalDing(0.8f));
                    yield return AnimateMedal(goldMedal, animationInterval * 2f, () => PlayMedalDing(0.9f));
                    yield return AnimateMedal(authorMedal, animationInterval, () => {
                        PlayMedalDing(1f);
                        medalAuthorAudio.Play();
                    });
                    break;

                case 3:
                    yield return AnimateMedal(bronzeMedal, animationInterval, () => PlayMedalDing(0.7f));
                    yield return AnimateMedal(silverMedal, animationInterval * 1.5f, () => PlayMedalDing(0.8f));
                    yield return AnimateMedal(goldMedal, animationInterval, () => PlayMedalDing(0.9f));
                    break;

                case 2:
                    yield return AnimateMedal(bronzeMedal, animationInterval, () => PlayMedalDing(0.7f));
                    yield return AnimateMedal(silverMedal, animationInterval, () => PlayMedalDing(0.8f));
                    break;
                case 1:
                    yield return AnimateMedal(bronzeMedal, animationInterval, () => PlayMedalDing(0.7f));
                    break;
            }

            yield return new WaitForSeconds(animationInterval * 2);
            newPersonalBest.SetActive(personalBest);

            if (previousResult > 0) {
                var delta = result - previousResult;
                resultDeltaText.text = TimeExtensions.TimeSecondsToString(delta);
                resultDeltaText.color = delta > 0 ? Color.red : Color.green;
            }

            if (personalBest && medalCount > 0) scoreCheerAudio.Play();

            if (!isValid) {
                resultNotValid.SetActive(true);
                yield return new WaitForSeconds(1.5f);
            }
        }

        private void PlayMedalDing(float pitch) {
            medalDingAudio.pitch = pitch;
            medalDingAudio.Play();
        }

        private IEnumerator AnimateMedal(GameObject medal, float interval, Action onImpact) {
            medal.SetActive(true);
            var medalTransform = medal.transform;
            var medalGroup = medal.GetComponent<CanvasGroup>();
            var targetPosition = medalTransform.localPosition;
            var startingPosition = targetPosition + new Vector3(0, 0, -750);

            // we want this to animate very quickly as it may happen four times in the best case result!

            // vr mode we use the z axis as the canvas is in world space. In pancake we use scale instead.
            // var vrEnabled = Game.Instance.IsVREnabled;
            var animationValue = 0f;

            while (animationValue < animationDuration) {
                animationValue += Time.deltaTime;
                medalTransform.localScale = Vector3.one * MathfExtensions.Remap(0, animationDuration, 2.5f, 1f, animationValue);
                medalTransform.localPosition =
                    Vector3.Lerp(startingPosition, targetPosition, MathfExtensions.Remap(0, animationDuration, 0, 1, animationValue));
                medalGroup.alpha = MathfExtensions.Remap(0, animationDuration, 0, 1, animationValue);
                yield return new WaitForEndOfFrame();
            }

            onImpact();
            medalThudAudio.Play();
            dustImpactEffect.SendEvent("OnPlay");
            yield return new WaitForSeconds(interval);
        }
    }
}