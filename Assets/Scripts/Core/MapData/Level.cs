using System.Collections.Generic;
using Core.Scores;
using Misc;
using UnityEngine;

namespace Core.MapData {
    public class Level : IFdEnum {
        private readonly string _jsonPath;

        private Level(int id, string name, string jsonPath, GameType gameType) {
            Id = id;
            Name = name;
            _jsonPath = jsonPath;
            GameType = gameType;
        }

        public static Level GentleStart => new(0, "A Gentle Start", "a-gentle-start", GameType.TimeTrial);
        public static Level AroundTheBlock => new(1, "Around The Block", "around-the-block", GameType.TimeTrial);
        public static Level HoldOnToYourStomach => new(2, "Hold on to your stomach", "hold-on-to-your-stomach", GameType.TimeTrial);
        public static Level AroundTheStation => new(3, "Around the station", "around-the-station", GameType.TimeTrial);
        public static Level SpeedIsHalfTheBattle => new(4, "Speed is Only Half the Battle", "speed-is-only-half-the-battle", GameType.TimeTrial);
        public static Level YouMightWannaHoldBack => new(5, "You Might Wanna Hold Back a Bit", "you-might-wanna-hold-back-a-bit", GameType.TimeTrial);
        public static Level DeathValley => new(6, "Death Valley", "death-valley", GameType.TimeTrial);
        public static Level YouHaveHeadlightsRight => new(7, "You Have Headlights, Right?", "you-have-headlights-right", GameType.TimeTrial);
        public static Level LimiterMastery => new(8, "Limiter Mastery", "limiter-mastery", GameType.TimeTrial);
        public GameType GameType { get; }
        public LevelData Data => LevelData.FromJsonString(Resources.Load<TextAsset>($"Levels/{_jsonPath}/level").text);
        public Sprite Thumbnail => Resources.Load<Sprite>($"Levels/{_jsonPath}/thumbnail");
        public Score Score => Score.ScoreForLevel(Data);

        public int Id { get; }
        public string Name { get; }

        public static IEnumerable<Level> List() {
            return new[] {
                GentleStart, AroundTheBlock, HoldOnToYourStomach, AroundTheStation, SpeedIsHalfTheBattle, YouMightWannaHoldBack, DeathValley,
                YouHaveHeadlightsRight, LimiterMastery
            };
        }

        public static Level FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static Level FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}