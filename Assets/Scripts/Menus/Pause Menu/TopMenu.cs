using System.Collections;
using System.Collections.Generic;
using Audio;
using Menus.Main_Menu;
using Menus.Options;
using UnityEngine;
using UnityEngine.UI;

public class TopMenu : MonoBehaviour
{
    [SerializeField] private SinglePlayerMenu singlePlayerMenu;
    [SerializeField] private OptionsMenu optionsMenu;
    [SerializeField] private Button defaultActiveButton;
    private Animator _animator;

    private void Awake() {
        this._animator = this.GetComponent<Animator>();
        defaultActiveButton.Select();
    }

    public void Hide() {
        this.gameObject.SetActive(false);
    }

    public void Show() {
        this.gameObject.SetActive(true);
        defaultActiveButton.Select();
        this._animator.SetBool("Open", true);
    }

    public void OpenSinglePlayerPanel() {
        AudioManager.Instance.Play("ui-dialog-open");
        singlePlayerMenu.Show();
        Hide();
    }

    public void OpenMultiPlayerMenu() {
        
    }
        
    public void OpenOptionsPanel() {
        AudioManager.Instance.Play("ui-dialog-open");
        optionsMenu.Show();
        Hide();
    }

    public void CloseOptionsPanel() {
        optionsMenu.Hide();
        Show();
    }
        
    public void OpenDiscordLink() {
        AudioManager.Instance.Play("ui-dialog-open");
        Application.OpenURL("https://discord.gg/4daSEUKZ6A");
    }

    public void Quit() {
        Game.Instance.QuitGame();
        AudioManager.Instance.Play("ui-cancel");
    }
}
