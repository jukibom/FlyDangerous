using System.Threading.Tasks;
using Audio;
using Core;
using Core.OnlineServices;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class ServerBrowserEntry : MonoBehaviour {
        [SerializeField] private LoadingSpinner loadingSpinner;
        [SerializeField] private Text serverName;
        [SerializeField] private Text gameMode;
        [SerializeField] private Text players;
        [SerializeField] private Button joinButton;
        private LobbyInfo _lobbyInfo;

        public string LobbyId { get; set; }

        public async Task Refresh() {
            if (joinButton != null) {
                loadingSpinner.gameObject.SetActive(true);
                serverName.text = "    RETRIEVING SERVER INFORMATION ...";
                gameMode.text = "";
                players.text = "";
                joinButton.gameObject.SetActive(false);

                if (FdNetworkManager.Instance.HasMultiplayerServices) {
                    _lobbyInfo = await FdNetworkManager.Instance.OnlineService!.Multiplayer!.GetLobbyInfo(LobbyId);
                    UpdateUI();
                }
            }
        }

        public void Join() {
            if (FdNetworkManager.Instance.HasMultiplayerServices) {
                var caller = GetComponentInParent<ServerBrowserMenu>();
                caller.Hide();

                var connectingDialog = caller.ConnectingDialog;
                connectingDialog.Open(caller);
                UIAudioManager.Instance.Play("ui-confirm");
                connectingDialog.Connect(caller.LobbyMenu, _lobbyInfo.connectionAddress);
            }
        }

        private void UpdateUI() {
            loadingSpinner.gameObject.SetActive(false);
            serverName.text = _lobbyInfo.name;
            gameMode.text = _lobbyInfo.gameMode;
            players.text = $"{_lobbyInfo.players} / {_lobbyInfo.playersMax}";
            joinButton.gameObject.SetActive(true);
        }
    }
}