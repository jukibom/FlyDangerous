using System.Linq;
using Core;
using Core.MapData;
using Menus.Main_Menu.Components;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Menus.Main_Menu {
    public class TimeTrialMenu : MenuBase {
        [SerializeField] private UIButton startButton;
        [SerializeField] private UIButton backButton;

        [SerializeField] private LevelSelectPanel levelSelectPanel;

        private void OnEnable() {
            levelSelectPanel.OnLevelSelectedEvent += OnLevelSelected;
            startButton.OnButtonSubmitEvent += OnStartSelected;
            backButton.OnButtonSubmitEvent += OnBackSelected;
            startButton.button.gameObject.SetActive(false);
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

        private void StartTimeTrial() {
            Game.Instance.loadedMainLevel = levelSelectPanel.SelectedLevel;
            Game.Instance.ActiveGameReplays = levelSelectPanel.SelectedReplays;
            FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, levelSelectPanel.SelectedLevel.Data);
        }

        protected override void OnOpen() {
            levelSelectPanel.LoadLevels(Level.List().ToList().FindAll(level => level.GameType == GameType.TimeTrial));
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