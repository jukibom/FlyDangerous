using Audio;
using Engine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class MultiPlayerMenu : MonoBehaviour {

        [SerializeField] private Button defaultActiveButton;
        [SerializeField] private TopMenu topMenu;
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
            AudioManager.Instance.Play("ui-cancel");
            topMenu.Show();
            Hide();
        }

        public void OpenHostPanel() {
            AudioManager.Instance.Play("ui-dialog-open");
        }

        public void OpenJoinPanel() {
            AudioManager.Instance.Play("ui-dialog-open");
            joinMenu.Show();
            Hide();
        }
    }
}