using System.Collections;
using Core;
using Core.Scores;
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

    public void Show(Score score, Score previousBest = null) {
        StartCoroutine(ShowEndResultsScreen(score, previousBest));
    }

    private IEnumerator ShowEndResultsScreen(Score score, Score previousBest = null) {
        yield return new WaitForSeconds(1f);
        yield return ShowMedalScreen(score, previousBest);
        // TODO: and the rest yay
    }

    private IEnumerator ShowMedalScreen(Score score, Score previousBest = null) {
        medalsScreen.gameObject.SetActive(true);

        var personalBest = score.PersonalBestTotalTime;
        var previousPersonalBest = previousBest is { HasPlayedPreviously: true } ? previousBest.PersonalBestTotalTime : 0;
        var isNewPersonalBest = previousBest is { HasPlayedPreviously: false } || previousBest?.PersonalBestTotalTime > personalBest;
        var levelData = Game.Instance.LoadedLevelData;

        var authorTargetTime = Score.AuthorTimeTarget(levelData);
        var goldTargetTime = Score.GoldTimeTarget(levelData);
        var silverTargetTime = Score.SilverTimeTarget(levelData);
        var bronzeTargetTime = Score.BronzeTimeTarget(levelData);

        uint medalCount = 0;
        if (personalBest < bronzeTargetTime)
            medalCount++;
        if (personalBest < silverTargetTime)
            medalCount++;
        if (personalBest < goldTargetTime)
            medalCount++;
        if (personalBest < authorTargetTime)
            medalCount++;

        yield return medalsScreen.ShowAnimation(medalCount, isNewPersonalBest, personalBest, previousPersonalBest);
    }
}