using System;
using Audio;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class DisconnectionDialog : MonoBehaviour {
        [SerializeField] private TopMenu topMenu;
        [SerializeField] private Text reasonText;
        
        public string Reason {
            get => reasonText.text;
            set => reasonText.text = value;
        }

        private Animator _animator;

        private void Awake() {
            _animator = GetComponent<Animator>();
        }
        
        public void Show() {
            gameObject.SetActive(true);
            _animator.SetBool("Open", true);
        }

        public void Hide() {
            gameObject.SetActive(false);
        }

        public void Close() {
            AudioManager.Instance.Play("ui-cancel");
            topMenu.Show();
            Hide();
        }
    }
}