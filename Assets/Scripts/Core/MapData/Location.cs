#nullable enable
using System.Collections.Generic;
using Misc;

namespace Core.MapData {
    public class Location : IFdEnum {
        private static int _id;

        // Declare locations here and add to the List() function below
        public static readonly Location TerrainV3 = new("Biome Terrain V2", "Mixed Terrain",
            "A terrain with multiple mixed environments blended together over very large distances with water mechanics", "TerrainV3", true);

        public static readonly Location TestSpaceStation = new("Space Station", "Space Station", "An enormous test space station asset", "SpaceStation", false);

        public static readonly Location Space = new("Space", "Space", "Empty space - literally nothing here", "Space", false);

        public static readonly Location Achelous8A = new("Achelous 8A", "Achelous 8A",
            "A ridiculously unlikely, spiky, and treacherous nightmare-terrain basically just here for the Newton's Gambit nutters.", "Achelous 8A", true);

        public static readonly Location ProvingGrounds = new("Proving Grounds", "Proving Grounds",
            "A testing scene used for staging new features and testing flight mechanics", "ProvingGrounds", false);

        public static readonly Location TerrainV1 = new("Flat World", "Mountains (Legacy)",
            "Terrain with peaks no higher than 2km - only here for compatibility with legacy maps", "TerrainV1", true);

        public static readonly Location TerrainV2 = new("Canyons", "Canyons (Legacy)",
            "Terrain with peaks of 8km and deep, straight canyon grooves - only here for compatibility with legacy maps", "TerrainV2", true);


        private Location(string name, string displayName, string description, string sceneToLoad, bool isTerrain) {
            Id = GenerateId;
            Name = name;
            DisplayName = displayName;
            Description = description;
            SceneToLoad = sceneToLoad;
            IsTerrain = isTerrain;
        }

        private static int GenerateId => _id++;

        public string SceneToLoad { get; }
        public bool IsTerrain { get; }
        public string Description { get; }
        public int Id { get; }
        public string Name { get; }
        public string DisplayName { get; }

        public static IEnumerable<Location> List() {
            return new[] { TerrainV3, TestSpaceStation, Space, Achelous8A, ProvingGrounds, TerrainV1, TerrainV2 };
        }

        public static Location FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static Location FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}