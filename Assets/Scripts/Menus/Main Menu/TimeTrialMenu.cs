using Core;
using FdUI;
using Menus.Main_Menu.Components;
using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;
// using UnityEditor.EditorUtility;

namespace Menus.Main_Menu {
    public class TimeTrialMenu : MenuBase {
        [SerializeField] private UIButton startButton;
        [SerializeField] private UIButton backButton;
        [SerializeField] private UIButton customLevelsButton;

        [SerializeField] private LevelSelectPanel levelSelectPanel;

        private void OnEnable() {
            levelSelectPanel.OnLevelSelectedEvent += OnLevelSelected;
            startButton.OnButtonSubmitEvent += OnStartSelected;
            backButton.OnButtonSubmitEvent += OnBackSelected;
            startButton.button.gameObject.SetActive(false);
            customLevelsButton.OnButtonSubmitEvent += OnCustomLevelsSelected;
            backButton.label.text = "CANCEL";
        }

        private void OnDisable() {
            levelSelectPanel.OnLevelSelectedEvent -= OnLevelSelected;
            startButton.OnButtonSubmitEvent -= OnStartSelected;
            backButton.OnButtonSubmitEvent -= OnBackSelected;
        }

        private void OnLevelSelected() {
            startButton.button.gameObject.SetActive(true);
            startButton.button.Select();
            backButton.label.text = "BACK";
        }

        private void OnStartSelected(UIButton button) {
            if (levelSelectPanel.SelectedLevel != null) {
                startButton.button.gameObject.SetActive(false);
                StartTimeTrial();
            }
        }

        private void OnBackSelected(UIButton button) {
            NavBack();
        }

        private void OnCustomLevelsSelected(UIButton button)
        {
            string customPath = System.IO.Path.Combine(Application.persistentDataPath, "CustomLevels");
            if (!Directory.Exists(customPath))
            {
                Directory.CreateDirectory(customPath);
            }

            Application.OpenURL(@"file://" + customPath);
        }

        private void StartTimeTrial()
        {
            Game.Instance.loadedMainLevel = levelSelectPanel.SelectedLevel;
            Game.Instance.ActiveGameReplays = levelSelectPanel.SelectedReplays;
            FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, levelSelectPanel.SelectedLevel.Data);
        }

        public override void OnCancel(BaseEventData eventData) {
            NavBack();
        }

        private void NavBack() {
            if (levelSelectPanel.SelectedLevel != null) {
                PlayCancelSound();
                startButton.button.gameObject.SetActive(false);
                levelSelectPanel.DeSelectLevel();
                backButton.label.text = "CANCEL";
            }

            else {
                Cancel();
            }
        }
    }
}