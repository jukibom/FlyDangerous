using Menus.Main_Menu.Components;
using UnityEngine;

public class RaceResultsScreen : MonoBehaviour {
    [SerializeField] private MedalsScreen medalsScreen;
    [SerializeField] private GameObject uploadScreen;
    [SerializeField] private LevelCompetitionPanel competitionPanel;
    [SerializeField] private GameObject uiButtons;

    public void Hide() {
        medalsScreen.gameObject.SetActive(false);
        uploadScreen.gameObject.SetActive(false);
        competitionPanel.gameObject.SetActive(false);
        uiButtons.gameObject.SetActive(false);
    }
}