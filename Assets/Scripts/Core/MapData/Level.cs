using System.Collections.Generic;
using Misc;
using UnityEngine;

namespace Core.MapData {
    public class Level : IFdEnum {
        public static Level Level1A => new Level(0, "Around the station", "around-the-station", GameType.TimeTrial);
        public static Level Level1B => new Level(1, "Around the station", "around-the-station", GameType.TimeTrial);
        public static Level Level1C => new Level(2, "Around the station", "around-the-station", GameType.TimeTrial);
        public static Level Level1D => new Level(3, "Around the station", "around-the-station", GameType.TimeTrial);
        
        public int Id { get; private set; }
        public string Name { get; private set; }
        public GameType GameType { get; private set; }
        
        private readonly string _jsonPath;
        public LevelData Data => LevelData.FromJsonString(Resources.Load<TextAsset>($"Levels/{_jsonPath}/level").text);
        public Sprite Thumbnail => Resources.Load<Sprite>($"Levels/{_jsonPath}/thumbnail");
        
        // TODO: Store and load these in a sane format somewhere ...
        public string PersonalBest => "Some amazing time";
        public int[] PersonalBestSplits => new[] { 500, 500, 500 };

        private Level(int id, string name, string jsonPath, GameType gameType) {
            Id = id;
            Name = name;
            _jsonPath = jsonPath;
            GameType = gameType;
        }
        
        public static IEnumerable<Level> List() {
            return new[] { Level1A, Level1B, Level1C, Level1D, };
        }

        public static Level FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static Level FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}