using System.Collections.Generic;
using Core.Scores;
using Misc;
using UnityEngine;

namespace Core.MapData {
    public class Level : IFdEnum {
        private static int _id;

        // LEGACY MAPS
        public static readonly Level GentleStart = new("A Gentle Start", "a-gentle-start", GameType.Sprint, true);
        public static readonly Level UpsAndDowns = new("Ups and Downs", "ups-and-downs", GameType.Sprint, true);
        public static readonly Level AroundTheBlock = new("Around The Block", "around-the-block", GameType.Sprint, true);
        public static readonly Level HoldOnToYourStomach = new("Hold on to your stomach", "hold-on-to-your-stomach", GameType.Sprint, true);
        public static readonly Level AroundTheStation = new("Around the station", "around-the-station", GameType.Sprint, true);
        public static readonly Level Snake = new("Snake", "snake", GameType.Sprint, true);
        public static readonly Level SpeedIsHalfTheBattle = new("Speed is Only Half the Battle", "speed-is-only-half-the-battle", GameType.Sprint, true);
        public static readonly Level YouMightWannaHoldBack = new("You Might Wanna Hold Back a Bit", "you-might-wanna-hold-back-a-bit", GameType.Sprint, true);
        public static readonly Level DeathValley = new("Death Valley", "death-valley", GameType.Sprint, true);
        public static readonly Level CrestLoop = new("Crest Loop", "crest-loop", GameType.Sprint, true);
        public static readonly Level YouHaveHeadlightsRight = new("You Have Headlights, Right?", "you-have-headlights-right", GameType.Sprint, true);
        public static readonly Level LimiterMastery = new("Limiter Mastery", "limiter-mastery", GameType.Sprint, true);
        public static readonly Level ThreadTheNeedle = new("Thread The Needle", "thread-the-needle", GameType.Sprint, true);
        public static readonly Level MountainSpiral = new("Mountain Spiral", "mountain-spiral", GameType.Sprint, true);
        public static readonly Level LongRoad = new("Long Road", "long-road", GameType.Sprint, true);
        public static readonly Level HideAndSeek = new("Hide and Seek", "hide-and-seek", GameType.Sprint, true);

        // GLORIOUS NEW WORLD MAPS (for how long? who knows!)
        // Sprints
        public static readonly Level YouHaveToStartSomewhere = new("You Have to Start Somewhere", "you-have-to-start-somewhere", GameType.Sprint);
        public static readonly Level ALittleVerticality = new("A Little Verticality", "a-little-verticality", GameType.Sprint);
        public static readonly Level TinyTrial = new("Tiny Trial", "tiny-trial", GameType.Sprint);
        public static readonly Level Slalom = new("Slalom", "slalom", GameType.Sprint);
        public static readonly Level RampingUp = new("Ramping Up", "ramping-up", GameType.Sprint);
        public static readonly Level Corkscrew = new("Corkscrew", "corkscrew", GameType.Sprint);
        public static readonly Level Sightseeing = new("Sightseeing", "sightseeing", GameType.Sprint);
        public static readonly Level Yeet = new("Yeet", "yeet", GameType.Sprint);
        public static readonly Level LoopDeLoop = new("Loop-de-loop", "loop-de-loop", GameType.Sprint);
        public static readonly Level DesertDash = new("Desert Dash", "desert-dash", GameType.Sprint);
        public static readonly Level Chute = new("Chute", "chute", GameType.Sprint);
        public static readonly Level Coaster = new("Coaster", "coaster", GameType.Sprint);
        public static readonly Level ALittleDip = new("A Little Dip", "a-little-dip", GameType.Sprint);
        public static readonly Level MarshMarathon = new("Marsh Marathon", "marsh-marathon", GameType.Sprint);
        public static readonly Level TightSqueeze = new("Tight Squeeze", "tight-squeeze", GameType.Sprint);
        public static readonly Level IslandHopping = new("Island Hopping", "island-hopping", GameType.Sprint);
        public static readonly Level Labyrinth = new("Labyrinth", "labyrinth", GameType.Sprint);
        public static readonly Level FreshHell = new("Fresh Hell", "fresh-hell", GameType.Sprint);

        // Laps
        public static readonly Level AroundTheStationV2 = new("Around the Station", "around-the-station-v2", GameType.Laps);
        public static readonly Level CoastlineCircuit = new("Coastline Circuit", "coastline-circuit", GameType.Laps);
        public static readonly Level Slipstream = new("Slipstream", "slipstream", GameType.Laps);

        // Puzzle
        public static readonly Level DecisionsDecisions = new("Decisions decisions", "decisions-decisions", GameType.Puzzle);

        private readonly string _jsonPath;

        private Level(string name, string jsonPath, GameType gameType, bool isLegacy = false) {
            Id = GenerateId;
            Name = name;
            _jsonPath = jsonPath;
            GameType = gameType;
            IsLegacy = isLegacy;
        }

        private static int GenerateId => _id++;
        public GameType GameType { get; }

        public LevelData Data => LevelData.FromJsonString(Resources.Load<TextAsset>($"Levels/{_jsonPath}/level").text);

        public bool IsLegacy { get; }

        public Sprite Thumbnail => Resources.Load<Sprite>($"Levels/{_jsonPath}/thumbnail");
        public Score Score => Score.ScoreForLevel(Data);

        public int Id { get; }
        public string Name { get; }

        public static IEnumerable<Level> List() {
            return new[] {
                // legacy
                GentleStart, UpsAndDowns, AroundTheBlock, HoldOnToYourStomach, AroundTheStation, Snake, SpeedIsHalfTheBattle, YouMightWannaHoldBack,
                DeathValley, CrestLoop, YouHaveHeadlightsRight, LimiterMastery, ThreadTheNeedle, MountainSpiral, LongRoad, HideAndSeek,
                // new sprints
                YouHaveToStartSomewhere, ALittleVerticality, TinyTrial, Slalom, RampingUp, Corkscrew, Sightseeing, Yeet, LoopDeLoop, DesertDash, Chute, Coaster,
                ALittleDip, MarshMarathon, TightSqueeze, IslandHopping, Labyrinth, FreshHell,
                // new laps
                AroundTheStationV2, CoastlineCircuit, Slipstream,
                // new puzzle
                DecisionsDecisions
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