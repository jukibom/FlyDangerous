using System.Collections;
using System.Collections.Generic;
using Audio;
using Menus.Main_Menu;
using Menus.Options;
using UnityEngine;
using UnityEngine.UI;
using Core;
using Mirror;

public class TopMenu : MonoBehaviour
{
    [SerializeField] private SinglePlayerMenu singlePlayerMenu;
    [SerializeField] private MultiPlayerMenu multiPlayerMenu;
    [SerializeField] private ProfileMenu profileMenu;
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
        NetworkServer.dontListen = true;
        FdNetworkManager.Instance.StartHost();
        Game.Instance.SessionStatus = SessionStatus.SinglePlayerMenu;
        
        UIAudioManager.Instance.Play("ui-dialog-open");
        singlePlayerMenu.Show();
        Hide();
    }

    public void OpenMultiPlayerMenu() {
        UIAudioManager.Instance.Play("ui-dialog-open");
        multiPlayerMenu.Show();
        Hide();
    }

    public void OpenProfileMenu() {
        UIAudioManager.Instance.Play("ui-dialog-open");
        profileMenu.Show();
        Hide();
    }
        
    public void OpenOptionsPanel() {
        UIAudioManager.Instance.Play("ui-dialog-open");
        optionsMenu.Show();
        Hide();
    }

    public void CloseOptionsPanel() {
        optionsMenu.Hide();
        Show();
    }
        
    public void OpenDiscordLink() {
        UIAudioManager.Instance.Play("ui-dialog-open");
        Application.OpenURL("https://discord.gg/4daSEUKZ6A");
    }

    public void Quit() {
        Game.Instance.QuitGame();
        UIAudioManager.Instance.Play("ui-cancel");
    }
}
