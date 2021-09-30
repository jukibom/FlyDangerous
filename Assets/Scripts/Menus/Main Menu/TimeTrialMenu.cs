using System.Linq;
using Audio;
using Core;
using Core.MapData;
using Core.Scores;
using Misc;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Menus.Main_Menu {
    public class TimeTrialMenu : MonoBehaviour {
        [SerializeField] private SinglePlayerMenu singlePlayerMenu;
        [SerializeField] private Button defaultActiveButton;
        [SerializeField] private Button startButton;
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
        
        private Level _level;

        private Animator _animator;
        private static readonly int open = Animator.StringToHash("Open");

        private void Awake() {
            _animator = GetComponent<Animator>();
            var levels = Level.List();
            foreach (var level in levels) {
                var levelButton = Instantiate(levelUIElementPrefab, levelPrefabContainer);
                levelButton.LevelData = level;
                levelButton.gameObject.GetComponent<Button>().onClick.AddListener(OnLevelSelected);
            }

            SetSelectedLevel(levels.First());
        }
        public void Hide() {
            gameObject.SetActive(false);
        }

        public void Show() {
            startButton.interactable = true;
            gameObject.SetActive(true);
            defaultActiveButton.Select();
            _animator.SetBool(open, true);
        }

        public void ClosePanel() {
            UIAudioManager.Instance.Play("ui-cancel");
            singlePlayerMenu.Show();
            Hide();
        }
        
        public void StartTimeTrial() {
            startButton.interactable = false;
            FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Singleplayer, _level.Data);
        }

        private void OnLevelSelected() {
            UIAudioManager.Instance.Play("ui-confirm");
            var selectedLevel = EventSystem.current.currentSelectedGameObject.GetComponent<LevelUIElement>();
            SetSelectedLevel(selectedLevel.LevelData);
        }

        private void SetSelectedLevel(Level level) {
            _level = level;
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
            
            // if user hasn't beaten platinum, hide it!
            platinumMedalContainer.gameObject.SetActive(score.HasPlayedPreviously &&  bestTime <= platinumTargetTime);
            
            // TODO: show a medal icon associated with users' time
        }
    }
}