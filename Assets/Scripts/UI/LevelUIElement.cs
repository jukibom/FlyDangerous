using Core.MapData;
using Core.Scores;
using UnityEngine;
using UnityEngine.UI;

public class LevelUIElement : MonoBehaviour {
    [SerializeField] private Text levelName;
    [SerializeField] private Image thumbnail;
    [SerializeField] private Image bronzeMedal;
    [SerializeField] private Image silverMedal;
    [SerializeField] private Image goldMedal;
    [SerializeField] private Image authorMedal;
    private Level _level;

    public Level Level {
        get => _level;
        set {
            _level = value;
            RefreshUIElements();
        }
    }

    private void RefreshUIElements() {
        levelName.text = Level.Name.ToUpper();
        thumbnail.sprite = Level.Thumbnail;

        var score = Level.Score;
        var personalBest = score.PersonalBestTotalTime;

        var platinumTargetTime = Level.Data.authorTimeTarget;
        var goldTargetTime = Score.GoldTimeTarget(Level.Data);
        var silverTargetTime = Score.SilverTimeTarget(Level.Data);
        var bronzeTargetTime = Score.BronzeTimeTarget(Level.Data);

        bronzeMedal.enabled = score.HasPlayedPreviously && personalBest < bronzeTargetTime;
        silverMedal.enabled = score.HasPlayedPreviously && personalBest < silverTargetTime;
        goldMedal.enabled = score.HasPlayedPreviously && personalBest < goldTargetTime;
        authorMedal.enabled = score.HasPlayedPreviously && personalBest < platinumTargetTime;
    }
}