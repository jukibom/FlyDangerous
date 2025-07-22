using System;
using Core;
using Core.MapData;
using Core.Player;
using Menus.Main_Menu.Components;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI.GameModes {
    public class StartPanel : MonoBehaviour {

        [SerializeField] private GameModeUIHandler gameModeUIHandler;
        [SerializeField] private Button defaultButton;
        [SerializeField] private LevelDetails levelDetails;
        private Level _level;
        private Action _onStart;
        
        public void Show(Level level, Action onStart) {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                player.User.DisableGameInput();
                player.User.pauseMenuEnabled = false;
                FindObjectOfType<InGameUI>()?.OnPauseToggle(true);
                Game.Instance.FreeCursor();
                player.User.EnableUIInput();
                player.User.ResetMouseToCentre();
                player.User.restartEnabled = true;
            }
            
            gameObject.SetActive(true);
            
            _level = level;
            _onStart = onStart;
            
            defaultButton.Select();
            levelDetails.Populate(level);
        }

        public void Hide() {
            gameObject.SetActive(false);
        }
        
        public void OnStart() {
            var player = FdPlayer.FindLocalShipPlayer;
            player?.User.DisableUIInput();
            FindObjectOfType<InGameUI>()?.OnPauseToggle(false);
            _onStart?.Invoke();
            _onStart = null;
            Hide();
            _onStart?.Invoke();
        }

        public void ShowLeaderboard() {
            gameModeUIHandler.ShowLeaderboards(_onStart, null, () => Show(_level, _onStart));
            Hide();
        }
    }
}
