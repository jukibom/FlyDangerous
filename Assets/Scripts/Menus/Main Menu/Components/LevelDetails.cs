using Core.MapData;
using Core.Scores;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class LevelDetails : MonoBehaviour {
        [SerializeField] private Text levelName;
        [SerializeField] private Text authorName;

        [SerializeField] private Image levelThumbnail;

        [SerializeField] private Text personalBest;
        [SerializeField] private Text platinumTarget;
        [SerializeField] private Text goldTarget;
        [SerializeField] private Text silverTarget;
        [SerializeField] private Text bronzeTarget;
        [SerializeField] private GameObject platinumMedalContainer;

        public void Populate(Level level) {
            levelName.text = level.Name.ToUpper();
            authorName.text = level.Data.author;
            levelThumbnail.sprite = level.Thumbnail;

            var score = level.Score;
            var bestTime = score.PersonalBestScore;
            personalBest.text = bestTime > 0 ? TimeExtensions.TimeSecondsToStringWithMilliseconds(bestTime) : "NONE";

            var platinumTargetTime = level.Data.authorTimeTarget;
            var goldTargetTime = Score.GoldTimeTarget(level.Data);
            var silverTargetTime = Score.SilverTimeTarget(level.Data);
            var bronzeTargetTime = Score.BronzeTimeTarget(level.Data);

            platinumTarget.text = TimeExtensions.TimeSecondsToStringWithMilliseconds(platinumTargetTime);
            goldTarget.text = TimeExtensions.TimeSecondsToStringWithMilliseconds(goldTargetTime);
            silverTarget.text = TimeExtensions.TimeSecondsToStringWithMilliseconds(silverTargetTime);
            bronzeTarget.text = TimeExtensions.TimeSecondsToStringWithMilliseconds(bronzeTargetTime);

            // if user hasn't beaten author time, hide it!
            platinumMedalContainer.gameObject.SetActive(score.HasPlayedPreviously && bestTime <= platinumTargetTime);
        }
    }
}