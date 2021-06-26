using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Engine;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class HostMenu : MonoBehaviour {
        [SerializeField] private Button defaultActiveButton;
        [SerializeField] private MultiPlayerMenu topMenu;
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

        public void Cancel() {
            AudioManager.Instance.Play("ui-cancel");
            topMenu.Show();
            Hide();
        }

        public void Join() {
            // OH GOD WHY THE PAIN MAKE IT STOP
        }
    }
}