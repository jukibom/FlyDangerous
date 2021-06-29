using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Engine;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class LobbyMenu : MonoBehaviour {
        private NetworkManagerLobby _networkManagerLobby;

        [Header("UI")]
        [SerializeField] private MultiPlayerMenu topMenu;
        [SerializeField] private UIButton startButton;
        [SerializeField] private Text headerText;
        [SerializeField] private Button defaultActiveButton;
        
        private Animator _animator;

        public UIButton StartButton => startButton;

        private void Awake() {
            _animator = GetComponent<Animator>();
        }

        private void Start() {
        }

        public void Show() {
            _networkManagerLobby = NetworkManagerLobby.singleton as NetworkManagerLobby;
            gameObject.SetActive(true);
            _animator.SetBool("Open", true);
            defaultActiveButton.Select();
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        public void JoinPlayer() {
            headerText.text = "MULTIPLAYER LOBBY";
        }

        public void StartHost() {
            headerText.text = "HOSTING LOBBY";
            _networkManagerLobby.StartHost();
        }

        public void StopHost() {
            _networkManagerLobby.StopHost();
        }

        public void CloseLobby() {
            // TODO: show a notification here
            AudioManager.Instance.Play("ui-cancel");
            topMenu.Show();
            Hide();
        }

        public void Cancel() {
            _networkManagerLobby.StopHost();
            CloseLobby();
        }
    }
}