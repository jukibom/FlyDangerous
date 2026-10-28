using Core.MapData;
using Core.Scores;
using Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Menus.Main_Menu.Components {
    public class LevelDetails : MonoBehaviour {
        [SerializeField] private TMPro.TMP_Text levelName;
        [SerializeField] private TMPro.TMP_Text authorName;

        [SerializeField] private Image levelThumbnail;

        [SerializeField] private TMPro.TMP_Text personalBest;
        [SerializeField] private TMPro.TMP_Text platinumTarget;
        [SerializeField] private TMPro.TMP_Text goldTarget;
        [SerializeField] private TMPro.TMP_Text silverTarget;
        [SerializeField] private TMPro.TMP_Text bronzeTarget;
        [SerializeField] private GameObject platinumMedalContainer;

        /// <summary>
        /// Populate with raw level data, typically from json - thumbnail optional. 
        /// </summary>
        public void Populate(LevelData levelData, Sprite thumbnailImage = null) {
            levelThumbnail.gameObject.SetActive(false);
            if (thumbnailImage != null) {
                levelThumbnail.sprite = thumbnailImage;
                levelThumbnail.gameObject.SetActive(true);
            }
            
            levelName.text = levelData.name.ToUpper();
            authorName.text = levelData.author;
            var score = Score.ScoreForLevel(levelData);
            
            var bestTime = score.PersonalBestScore;
            personalBest.text = bestTime > 0 ? TimeExtensions.TimeSecondsToStringWithMilliseconds(bestTime) : "NONE";

            var platinumTargetTime = levelData.authorTimeTarget;
            var goldTargetTime = Score.GoldTimeTarget(levelData);
            var silverTargetTime = Score.SilverTimeTarget(levelData);
            var bronzeTargetTime = Score.BronzeTimeTarget(levelData);

            platinumTarget.text = TimeExtensions.TimeSecondsToStringWithMilliseconds(platinumTargetTime);
            goldTarget.text = TimeExtensions.TimeSecondsToStringWithMilliseconds(goldTargetTime);
            silverTarget.text = TimeExtensions.TimeSecondsToStringWithMilliseconds(silverTargetTime);
            bronzeTarget.text = TimeExtensions.TimeSecondsToStringWithMilliseconds(bronzeTargetTime);

            // if user hasn't beaten author time, hide it!
            platinumMedalContainer.gameObject.SetActive(score.HasPlayedPreviously && bestTime <= platinumTargetTime);
        }
        
        /// <summary>
        /// Populate from canonical level data, packaged with a thumbnail.
        /// </summary>
        /// <param name="level"></param>
        public void Populate(Level level) {
            Populate(level.Data, level.Thumbnail);
        }
    }
}