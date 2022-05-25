using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Core.MapData;
using Core.Replays;
using Core.Scores;
using Den.Tools;
using Misc;
using UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Menus.Main_Menu.Components {
    public class LevelSelectPanel : MonoBehaviour {
        public delegate void OnLevelSelectedAction();

        [SerializeField] private LevelUIElement levelUIElementPrefab;
        [SerializeField] private RectTransform levelPrefabContainer;

        [SerializeField] private Text levelName;
        [SerializeField] private Image levelThumbnail;

        [SerializeField] private Text personalBest;
        [SerializeField] private Text platinumTarget;
        [SerializeField] private Text goldTarget;
        [SerializeField] private Text silverTarget;
        [SerializeField] private Text bronzeTarget;
        [SerializeField] private GameObject platinumMedalContainer;

        [SerializeField] private LayoutElement levelGridLayoutElement;
        [SerializeField] private LayoutElement summaryScreenGridLayoutElement;
        [SerializeField] private LevelCompetitionPanel competitionPanel;

        [SerializeField] private FlowLayoutGroup levelFlowLayoutGroup;
        [SerializeField] private AnimationCurve screenTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float panelAnimationTimeSeconds = 0.5f;
        private readonly float openPanelPreferredWidthValue = 2000;
        private Coroutine _panelAnimationHideCoroutine;
        private Coroutine _panelAnimationShowCoroutine;

        public Level SelectedLevel { get; private set; }
        public List<Replay> SelectedReplays => competitionPanel.GetSelectedReplays();
        public event OnLevelSelectedAction OnLevelSelectedEvent;

        public void LoadLevels(List<Level> levels) {
            foreach (var levelUI in levelPrefabContainer.gameObject.GetComponentsInChildren<LevelUIElement>()) Destroy(levelUI.gameObject);
            foreach (var level in levels) {
                Debug.Log($"Loaded level {level.Name}: {level.Data.LevelHash()}");
                var levelButton = Instantiate(levelUIElementPrefab, levelPrefabContainer);
                levelButton.Level = level;
                levelButton.gameObject.GetComponent<UIButton>().OnButtonSubmitEvent += OnLevelSelected;
                levelButton.gameObject.GetComponent<UIButton>().OnButtonSelectEvent += OnLevelHighLighted;
                levelButton.gameObject.GetComponent<UIButton>().OnButtonHighlightedEvent += OnLevelHighLighted;
                levelButton.gameObject.GetComponent<UIButton>().OnButtonUnHighlightedEvent += OnLevelUnHighLighted;
            }

            levelFlowLayoutGroup.enabled = true;
            levelGridLayoutElement.preferredWidth = 2000;
            summaryScreenGridLayoutElement.preferredWidth = 0;

            levelGridLayoutElement.gameObject.SetActive(true);
            summaryScreenGridLayoutElement.gameObject.SetActive(false);

            // Select the first level on load
            // Yes, this is what giving up looks like.
            // Don't you judge me.
            IEnumerator SelectFirst() {
                yield return new WaitForEndOfFrame();
                var firstLevel = levelPrefabContainer.GetComponentsInChildren<LevelUIElement>().First();
                if (firstLevel != null) firstLevel.GetComponent<Button>().Select();
            }

            StartCoroutine(SelectFirst());
        }

        private void OnLevelHighLighted(UIButton uiButton) {
            HighlightSelectedLevel(uiButton.GetComponent<LevelUIElement>().Level);
        }

        private void OnLevelUnHighLighted(UIButton uiButton) {
            HighlightSelectedLevel(SelectedLevel);
        }

        private void OnLevelSelected(UIButton uiButton) {
            var level = uiButton.GetComponent<LevelUIElement>().Level;
            HighlightSelectedLevel(level);
            UIAudioManager.Instance.Play("ui-dialog-open");
            SetSelectedLevel(level);
        }

        private void HighlightSelectedLevel(Level level) {
            if (level != null && SelectedLevel == null) {
                levelName.text = level.Name;
                levelThumbnail.sprite = level.Thumbnail;

                var score = level.Score;
                var bestTime = score.PersonalBestTotalTime;
                personalBest.text = bestTime > 0 ? TimeExtensions.TimeSecondsToString(bestTime) : "NONE";

                var platinumTargetTime = level.Data.authorTimeTarget;
                var goldTargetTime = Score.GoldTimeTarget(level.Data);
                var silverTargetTime = Score.SilverTimeTarget(level.Data);
                var bronzeTargetTime = Score.BronzeTimeTarget(level.Data);

                platinumTarget.text = TimeExtensions.TimeSecondsToString(platinumTargetTime);
                goldTarget.text = TimeExtensions.TimeSecondsToString(goldTargetTime);
                silverTarget.text = TimeExtensions.TimeSecondsToString(silverTargetTime);
                bronzeTarget.text = TimeExtensions.TimeSecondsToString(bronzeTargetTime);

                // if user hasn't beaten author time, hide it!
                platinumMedalContainer.gameObject.SetActive(score.HasPlayedPreviously && bestTime <= platinumTargetTime);
            }
        }

        public void DeSelectLevel() {
            SwitchToLevelSelectScreen(() => {
                levelFlowLayoutGroup.enabled = true;
                // select the previous level if there is one
                if (SelectedLevel != null)
                    levelPrefabContainer.GetComponentsInChildren<LevelUIElement>()
                        .FindMember(levelButton => levelButton.Level == SelectedLevel)
                        ?.GetComponent<Button>()
                        ?.Select();
                SelectedLevel = null;
            });
        }

        private void SetSelectedLevel(Level level) {
            SelectedLevel = level;
            SwitchToSummaryScreen();
            competitionPanel.Populate(SelectedLevel.Data);
        }

        private void SwitchToSummaryScreen() {
            levelFlowLayoutGroup.enabled = false;
            if (_panelAnimationHideCoroutine != null) StopCoroutine(_panelAnimationHideCoroutine);
            if (_panelAnimationShowCoroutine != null) StopCoroutine(_panelAnimationShowCoroutine);
            OnLevelSelectedEvent?.Invoke();
            _panelAnimationHideCoroutine = StartCoroutine(HidePanel(levelGridLayoutElement));
            _panelAnimationShowCoroutine = StartCoroutine(ShowPanel(summaryScreenGridLayoutElement));
        }

        private void SwitchToLevelSelectScreen(Action onComplete = null) {
            if (_panelAnimationHideCoroutine != null) StopCoroutine(_panelAnimationHideCoroutine);
            if (_panelAnimationShowCoroutine != null) StopCoroutine(_panelAnimationShowCoroutine);
            _panelAnimationHideCoroutine = StartCoroutine(HidePanel(summaryScreenGridLayoutElement));
            _panelAnimationShowCoroutine = StartCoroutine(ShowPanel(levelGridLayoutElement, onComplete));
        }

        private IEnumerator HidePanel(LayoutElement panel, Action onComplete = null) {
            var frameIncrement = Time.fixedDeltaTime / panelAnimationTimeSeconds;

            var animationPosition = 0f;
            while (animationPosition <= 1) {
                panel.preferredWidth = MathfExtensions.Remap(0, 1, openPanelPreferredWidthValue, 0, screenTransitionCurve.Evaluate(animationPosition));
                animationPosition += frameIncrement;
                yield return new WaitForFixedUpdate();
            }

            panel.gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        private IEnumerator ShowPanel(LayoutElement panel, Action onComplete = null) {
            var frameIncrement = Time.fixedDeltaTime / panelAnimationTimeSeconds;

            panel.gameObject.SetActive(true);
            var animationPosition = 0f;
            while (animationPosition <= 1) {
                panel.preferredWidth = MathfExtensions.Remap(0, 1, 0, openPanelPreferredWidthValue, screenTransitionCurve.Evaluate(animationPosition));
                animationPosition += frameIncrement;
                yield return new WaitForFixedUpdate();
            }

            onComplete?.Invoke();
        }
    }
}