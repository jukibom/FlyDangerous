using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Audio;
using Core.MapData;
using Core.Replays;
using Core.Scores;
using Den.Tools;
using FdUI;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class LevelSelectPanel : MonoBehaviour {
        public delegate void OnLevelSelectedAction();

        [SerializeField] private TabGroup tabGroup;
        [SerializeField] private Text footer;
        [SerializeField] private LevelUIElement levelUIElementPrefab;
        [SerializeField] private RectTransform levelPrefabContainer;
        [SerializeField] private LevelDetails levelDetails;
        
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

        private void OnEnable() {
            tabGroup.OnTabSelected += OnLevelGroupTabSelected;

            // try to find the first level in the list and select it if already loaded (e.g. returning to this menu)
            var firstLevel = levelPrefabContainer.GetComponentsInChildren<LevelUIElement>().First();
            if (firstLevel != null) firstLevel.GetComponent<Button>().Select();
        }

        private void OnDisable() {
            tabGroup.OnTabSelected -= OnLevelGroupTabSelected;
        }

        private void OnLevelGroupTabSelected(string tabId) {
            switch (tabId) {
                case "sprint":
                    LoadLevels(Level.List().ToList().FindAll(level => level.GameType == GameType.Sprint && !level.IsLegacy));
                    footer.text = "FOLLOW THE PATH, HIT EVERY CHECKPOINT, GET TO THE END.\nORDER OF CHECKPOINTS DOESN'T STRICTLY MATTER.";
                    break;
                case "laps":
                    LoadLevels(Level.List().ToList().FindAll(level => level.GameType == GameType.Laps && !level.IsLegacy));
                    footer.text = "FOLLOW THE CIRCUIT, HIT EVERY CHECKPOINT, COMPLETE ALL LAPS.\nORDER OF CHECKPOINTS DOESN'T STRICTLY MATTER.";
                    break;
                case "puzzle":
                    LoadLevels(Level.List().ToList().FindAll(level => level.GameType == GameType.Puzzle && !level.IsLegacy));
                    footer.text = "NO DEFINED PATH AND NO DEFINED END, FIND THE FASTEST ROUTE.";
                    break;
                case "legacy":
                    LoadLevels(Level.List().ToList().FindAll(level => level.GameType == GameType.Sprint && level.IsLegacy));
                    footer.text = "THESE ARE OLD SPRINT MAPS AND MAY BE IMPOSSIBLE TO BEAT THE LEADERBOARD!\nHERE FOR POSTERITY.";
                    break;
                case "custom":
                    LoadLevels(Level.ListCustom());
                    footer.text = $"CUSTOM MAPS LOADED FROM {Path.Combine(Application.persistentDataPath, "CustomLevels").Replace("\\", "/")}.";
                    break;
            }
        }

        public void LoadLevels(List<Level> levels) {
            foreach (var levelUI in levelPrefabContainer.gameObject.GetComponentsInChildren<LevelUIElement>()) Destroy(levelUI.gameObject);

            levelFlowLayoutGroup.enabled = true;
            levelGridLayoutElement.preferredWidth = 2000;
            summaryScreenGridLayoutElement.preferredWidth = 0;

            levelGridLayoutElement.gameObject.SetActive(true);
            summaryScreenGridLayoutElement.gameObject.SetActive(false);

            // Load level panels one at a time then select the first one
            IEnumerator AddLevelPanels() {
                foreach (var level in levels) {
                    Debug.Log($"Loaded level {level.Name}: {level.Data.LevelHash()}");
                    var levelButton = Instantiate(levelUIElementPrefab, levelPrefabContainer);
                    levelButton.Level = level;
                    levelButton.gameObject.GetComponent<UIButton>().OnButtonSubmitEvent += OnLevelSelected;
                    levelButton.gameObject.GetComponent<UIButton>().OnButtonSelectEvent += OnLevelHighLighted;
                    levelButton.gameObject.GetComponent<UIButton>().OnButtonHighlightedEvent += OnLevelHighLighted;
                    levelButton.gameObject.GetComponent<UIButton>().OnButtonUnHighlightedEvent += OnLevelUnHighLighted;

                    yield return new WaitForEndOfFrame();
                }

                var firstLevel = levelPrefabContainer.GetComponentsInChildren<LevelUIElement>().First();
                if (firstLevel != null) firstLevel.GetComponent<Button>().Select();
            }

            StartCoroutine(AddLevelPanels());
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
                levelDetails.Populate(level);
            }
        }

        public void DeSelectLevel() {
            levelFlowLayoutGroup.enabled = true;
            SwitchToLevelSelectScreen(() => {
                levelFlowLayoutGroup.GetComponentInParent<ScrollRect>().enabled = true;

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
            levelFlowLayoutGroup.GetComponentInParent<ScrollRect>().enabled = false;

            if (_panelAnimationHideCoroutine != null) StopCoroutine(_panelAnimationHideCoroutine);
            if (_panelAnimationShowCoroutine != null) StopCoroutine(_panelAnimationShowCoroutine);
            OnLevelSelectedEvent?.Invoke();
            _panelAnimationHideCoroutine = StartCoroutine(HidePanel(levelGridLayoutElement));
            _panelAnimationShowCoroutine = StartCoroutine(ShowPanel(summaryScreenGridLayoutElement));
        }

        private void SwitchToLevelSelectScreen(Action onComplete = null) {
            competitionPanel.ClearLeaderboard();
            competitionPanel.ClearGhosts();
            if (_panelAnimationHideCoroutine != null) StopCoroutine(_panelAnimationHideCoroutine);
            if (_panelAnimationShowCoroutine != null) StopCoroutine(_panelAnimationShowCoroutine);
            _panelAnimationHideCoroutine = StartCoroutine(HidePanel(summaryScreenGridLayoutElement));
            _panelAnimationShowCoroutine = StartCoroutine(ShowPanel(levelGridLayoutElement, onComplete));
        }

        private IEnumerator HidePanel(LayoutElement panel, Action onComplete = null) {
            var frameIncrement = Time.fixedDeltaTime / panelAnimationTimeSeconds;

            var animationPosition = 0f;
            while (animationPosition <= 1) {
                panel.preferredWidth = screenTransitionCurve.Evaluate(animationPosition).Remap(0, 1, openPanelPreferredWidthValue, 0);
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
                panel.preferredWidth = screenTransitionCurve.Evaluate(animationPosition).Remap(0, 1, 0, openPanelPreferredWidthValue);
                animationPosition += frameIncrement;
                yield return new WaitForFixedUpdate();
            }

            onComplete?.Invoke();
        }
    }
}