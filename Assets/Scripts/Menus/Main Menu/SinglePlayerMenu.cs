using Audio;
using Core;
using Core.MapData;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu {
    public class SinglePlayerMenu : MonoBehaviour {

        [SerializeField] private Button defaultActiveButton;
        [SerializeField] private TopMenu topMenu;
        [SerializeField] private TimeTrialMenu timeTrialMenu;
        [SerializeField] private FreeRoamMenu freeRoamMenu;
        [SerializeField] private LoadCustomMenu loadCustomMenu;
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

        public void Cancel() {
            Game.Instance.SessionStatus = SessionStatus.Offline;
            FdNetworkManager.Instance.StopAll();
            ClosePanel();
        }

        public void OpenCampaignPanel() {
            Debug.Log("Nope not yet!");
        }

        public void OpenTimeTrialPanel() {
            UIAudioManager.Instance.Play("ui-confirm");
            timeTrialMenu.Show();
            Hide();
        }

        public void OpenFreeRoamPanel() {
            UIAudioManager.Instance.Play("ui-dialog-open");
            freeRoamMenu.Show();
            Hide();
        }

        public void OpenLoadCustomPanel() {
            UIAudioManager.Instance.Play("ui-dialog-open");
            loadCustomMenu.Show();
            Hide();
        }
    }
}