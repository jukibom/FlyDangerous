using System.Collections;
using System.Collections.Generic;
using Audio;
using Menus.Main_Menu;
using Menus.Options;
using UnityEngine;
using UnityEngine.UI;
using Core;

public class TopMenu : MonoBehaviour
{
    [SerializeField] private SinglePlayerMenu singlePlayerMenu;
    [SerializeField] private MultiPlayerMenu multiPlayerMenu;
    [SerializeField] private OptionsMenu optionsMenu;
    [SerializeField] private Button defaultActiveButton;
    private Animator _animator;

    private void Awake() {
        _animator = GetComponent<Animator>();
        defaultActiveButton.Select();
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    public void Show() {
        gameObject.SetActive(true);
        defaultActiveButton.Select();
        _animator.SetBool("Open", true);
    }

    public void OpenSinglePlayerPanel() {
        FdNetworkManager.Instance.StartOfflineServer();
        AudioManager.Instance.Play("ui-dialog-open");
        singlePlayerMenu.Show();
        Hide();
    }

    public void OpenMultiPlayerMenu() {
        AudioManager.Instance.Play("ui-dialog-open");
        multiPlayerMenu.Show();
        Hide();
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
