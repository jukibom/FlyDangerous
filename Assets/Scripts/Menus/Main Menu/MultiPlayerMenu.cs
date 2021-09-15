using Audio;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class MultiPlayerMenu : MonoBehaviour {

        [SerializeField] private Button defaultActiveButton;
        [SerializeField] private TopMenu topMenu;
        [SerializeField] private LobbyMenu lobbyMenu;
        [SerializeField] private JoinMenu joinMenu;
        private Animator _animator;
        
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

        public void ClosePanel() {
            UIAudioManager.Instance.Play("ui-cancel");
            topMenu.Show();
            Hide();
        }

        public void OpenHostPanel() {
            Game.Instance.SessionStatus = SessionStatus.LobbyMenu;
            UIAudioManager.Instance.Play("ui-dialog-open");
            lobbyMenu.Show();
            lobbyMenu.StartHost();
            Hide();
        }

        public void OpenJoinPanel() {
            UIAudioManager.Instance.Play("ui-dialog-open");
            joinMenu.Show();
            Hide();
        }
    }
}