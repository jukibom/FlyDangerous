using System.Linq;
using Audio;
using Core;
using Core.MapData;
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
            Debug.Log($"{selectedLevel.LevelData.Name} ({selectedLevel.LevelData.Id}) selected");
        }

        private void SetSelectedLevel(Level level) {
            _level = level;
            levelName.text = level.Name;
            levelThumbnail.sprite = level.Thumbnail;

            // TODO: Personal bests
        }

    }
}