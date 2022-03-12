#nullable enable
using System.Collections.Generic;
using Misc;

namespace Core.MapData {
    public class Location : IFdEnum {
        private Location(int id, string name, string sceneToLoad, bool isTerrain) {
            Id = id;
            Name = name;
            SceneToLoad = sceneToLoad;
            IsTerrain = isTerrain;
        }

        // Declare locations here and add to the List() function below
        public static Location Space => new(0, "Space", "Space", false);
        public static Location TestSpaceStation => new(1, "Space Station", "SpaceStation", false);
        public static Location TerrainV1 => new(2, "Flat World", "TerrainV1", true);
        public static Location TerrainV2 => new(3, "Canyons", "TerrainV2", true);
        public static Location TerrainV3 => new(4, "Biome World", "TerrainV3", true);
        public string SceneToLoad { get; }
        public bool IsTerrain { get; }


        public int Id { get; }
        public string Name { get; }

        public static IEnumerable<Location> List() {
            return new[] { Space, TestSpaceStation, TerrainV1, TerrainV2, TerrainV3 };
        }

        public static Location FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static Location FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}