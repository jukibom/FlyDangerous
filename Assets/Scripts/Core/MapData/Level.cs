using System.Collections.Generic;
using Core.Scores;
using Misc;
using UnityEngine;

namespace Core.MapData {
    public class Level : IFdEnum {
        public static Level GentleStart => new Level(0, "A Gentle Start", "a-gentle-start", GameType.TimeTrial);
        public static Level HoldOnToYourStomach => new Level(1, "Hold on to your stomach", "hold-on-to-your-stomach", GameType.TimeTrial);
        public static Level AroundTheStation => new Level(2, "Around the station", "around-the-station", GameType.TimeTrial);
        public static Level SpeedIsHalfTheBattle => new Level(3, "Speed is Only Half the Battle", "speed-is-only-half-the-battle", GameType.TimeTrial);
        public static Level YouMightWannaHoldBack => new Level(3, "You Might Wanna Hold Back a Bit", "you-might-wanna-hold-back-a-bit", GameType.TimeTrial);
        public static Level LimiterMastery => new Level(3, "Limiter Mastery", "limiter-mastery", GameType.TimeTrial);

        public int Id { get; }
        public string Name { get; }
        public GameType GameType { get; }
        
        private readonly string _jsonPath;
        public LevelData Data => LevelData.FromJsonString(Resources.Load<TextAsset>($"Levels/{_jsonPath}/level").text);
        public Sprite Thumbnail => Resources.Load<Sprite>($"Levels/{_jsonPath}/thumbnail");
        public Score Score => Score.ScoreForLevel(Data);

        private Level(int id, string name, string jsonPath, GameType gameType) {
            Id = id;
            Name = name;
            _jsonPath = jsonPath;
            GameType = gameType;
        }
        
        public static IEnumerable<Level> List() {
            return new[] { GentleStart, HoldOnToYourStomach, AroundTheStation, SpeedIsHalfTheBattle, YouMightWannaHoldBack, LimiterMastery };
        }

        public static Level FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static Level FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}