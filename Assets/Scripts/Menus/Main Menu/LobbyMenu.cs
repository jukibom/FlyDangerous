using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Engine;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class LobbyMenu : MonoBehaviour {
        [SerializeField] private NetworkManagerLobby networkManagerLobby;
        [SerializeField] private MultiPlayerMenu topMenu;

        [Header("UI")]
        [SerializeField] private Text headerText;
        [SerializeField] private Button defaultActiveButton;
        
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

        public void JoinPlayer() {
            headerText.text = "MULTIPLAYER LOBBY";
        }

        public void StartHost() {
            headerText.text = "HOSTING LOBBY";
            networkManagerLobby.StartHost();
        }

        public void StopHost() {
            networkManagerLobby.StopHost();
        }

        public void Cancel() {
            AudioManager.Instance.Play("ui-cancel");
            topMenu.Show();
            networkManagerLobby.StopHost();
            Hide();
        }
    }
}