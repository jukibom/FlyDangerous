using Audio;
using Core;
using Core.Player;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using Environment = Core.Environment;

namespace Menus.Main_Menu {
    public class LobbyMenu : MonoBehaviour {

        [Header("UI")]
        [SerializeField] private MultiPlayerMenu topMenu;
        [SerializeField] private UIButton startButton;
        [SerializeField] private Text headerText;
        [SerializeField] private Button defaultActiveButton;

        [SerializeField] private Button loadCustomButton;
        [SerializeField] private LobbyConfigurationPanel lobbyConfigurationPanel;

        private Animator _animator;

        public UIButton StartButton => startButton;
        
        private void Awake() {
            _animator = GetComponent<Animator>();
        }

        public void Show() {
            gameObject.SetActive(true);
            _animator.SetBool("Open", true);
            defaultActiveButton.Select();
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        public void JoinPlayer() {
            headerText.text = "MULTIPLAYER LOBBY";
            lobbyConfigurationPanel.IsHost = NetworkClient.isHostClient;
        }

        public void StartHost() {
            headerText.text = "HOSTING LOBBY";
            FdNetworkManager.Instance.StartLobbyServer();
        }

        public void StopHost() {
            FdNetworkManager.Instance.StopHost();
        }

        public void StartGame() {
            var localLobbyPlayer = LobbyPlayer.FindLocal;
            var lobbyLevelData = lobbyConfigurationPanel.LobbyLevelData;
            if (localLobbyPlayer && localLobbyPlayer.isHost) {
                FdNetworkManager.Instance.StartGameLoadSequence(SessionType.Multiplayer, lobbyLevelData, true);
            }
        }

        public void CloseLobby() {
            // TODO: show a notification here
            AudioManager.Instance.Play("ui-cancel");
            topMenu.Show();
            Hide();
        }

        public void Cancel() {
            FdNetworkManager.Instance.StopAll();
            CloseLobby();
        }
    }
}