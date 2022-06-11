using System.Collections.Generic;
using Core.Scores;
using Misc;
using UnityEngine;

namespace Core.MapData {
    public class Level : IFdEnum {
        private static int _id;

        public static readonly Level GentleStart = new("A Gentle Start", "a-gentle-start", GameType.TimeTrial);
        public static readonly Level UpsAndDowns = new("Ups and Downs", "ups-and-downs", GameType.TimeTrial);
        public static readonly Level AroundTheBlock = new("Around The Block", "around-the-block", GameType.TimeTrial);
        public static readonly Level HoldOnToYourStomach = new("Hold on to your stomach", "hold-on-to-your-stomach", GameType.TimeTrial);
        public static readonly Level AroundTheStation = new("Around the station", "around-the-station", GameType.TimeTrial);
        public static readonly Level Snake = new("Snake", "snake", GameType.TimeTrial);
        public static readonly Level SpeedIsHalfTheBattle = new("Speed is Only Half the Battle", "speed-is-only-half-the-battle", GameType.TimeTrial);
        public static readonly Level YouMightWannaHoldBack = new("You Might Wanna Hold Back a Bit", "you-might-wanna-hold-back-a-bit", GameType.TimeTrial);
        public static readonly Level DeathValley = new("Death Valley", "death-valley", GameType.TimeTrial);
        public static readonly Level CrestLoop = new("Crest Loop", "crest-loop", GameType.TimeTrial);
        public static readonly Level YouHaveHeadlightsRight = new("You Have Headlights, Right?", "you-have-headlights-right", GameType.TimeTrial);
        public static readonly Level LimiterMastery = new("Limiter Mastery", "limiter-mastery", GameType.TimeTrial);
        public static readonly Level ThreadTheNeedle = new("Thread The Needle", "thread-the-needle", GameType.TimeTrial);
        public static readonly Level MountainSpiral = new("Mountain Spiral", "mountain-spiral", GameType.TimeTrial);
        public static readonly Level LongRoad = new("Long Road", "long-road", GameType.TimeTrial);
        public static readonly Level HideAndSeek = new("Hide and Seek", "hide-and-seek", GameType.TimeTrial);
        private readonly string _jsonPath;

        private Level(string name, string jsonPath, GameType gameType) {
            Id = GenerateId;
            Name = name;
            _jsonPath = jsonPath;
            GameType = gameType;
        }

        private static int GenerateId => _id++;
        public GameType GameType { get; }
        public LevelData Data => LevelData.FromJsonString(Resources.Load<TextAsset>($"Levels/{_jsonPath}/level").text);
        public Sprite Thumbnail => Resources.Load<Sprite>($"Levels/{_jsonPath}/thumbnail");
        public Score Score => Score.ScoreForLevel(Data);

        public int Id { get; }
        public string Name { get; }

        public static IEnumerable<Level> List() {
            return new[] {
                GentleStart, UpsAndDowns, AroundTheBlock, HoldOnToYourStomach, AroundTheStation, Snake, SpeedIsHalfTheBattle, YouMightWannaHoldBack,
                DeathValley, CrestLoop,
                YouHaveHeadlightsRight, LimiterMastery, ThreadTheNeedle, MountainSpiral, LongRoad, HideAndSeek
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