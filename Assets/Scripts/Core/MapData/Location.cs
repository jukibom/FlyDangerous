#nullable enable
using System.Collections.Generic;
using Misc;

namespace Core.MapData {
    public class Location : IFdEnum {
        private static int _id;

        // Declare locations here and add to the List() function below
        public static readonly Location Space = new("Space", "Space", false);
        public static readonly Location TestSpaceStation = new("Space Station", "SpaceStation", false);
        public static readonly Location TerrainV1 = new("Flat World", "TerrainV1", true);
        public static readonly Location TerrainV2 = new("Canyons", "TerrainV2", true);
        public static readonly Location TerrainV3 = new("Biome World", "TerrainV3", true);
        public static readonly Location TerrainGPUFoliageTest = new("GPU Foliage Test", "TerrainWorkspace", true);

        private Location(string name, string sceneToLoad, bool isTerrain) {
            Id = GenerateId;
            Name = name;
            SceneToLoad = sceneToLoad;
            IsTerrain = isTerrain;
        }

        private static int GenerateId => _id++;

        public string SceneToLoad { get; }
        public bool IsTerrain { get; }


        public int Id { get; }
        public string Name { get; }

        public static IEnumerable<Location> List() {
            return new[] { Space, TestSpaceStation, TerrainV1, TerrainV2, TerrainV3, TerrainGPUFoliageTest };
        }

        public static Location FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static Location FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}