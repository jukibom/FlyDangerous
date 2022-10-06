#nullable enable
using System.Collections.Generic;
using Misc;

namespace Core.MapData {
    public class Location : IFdEnum {
        private static int _id;

        // Declare locations here and add to the List() function below
        public static readonly Location ProvingGrounds = new("Proving Grounds",
            "A testing scene used for staging new features and testing flight mechanics", "ProvingGrounds", false);

        public static readonly Location Space = new("Space", "Empty space - literally nothing here", "Space", false);
        public static readonly Location TestSpaceStation = new("Space Station", "An enormous test space station asset", "SpaceStation", false);
        public static readonly Location TerrainV1 = new("Flat World", "Terrain with peaks no higher than 2km", "TerrainV1", true);
        public static readonly Location TerrainV2 = new("Canyons", "Terrain with peaks of 8km and deep, straight canyon grooves", "TerrainV2", true);
        public static readonly Location TerrainV3 = new("Biome World", "A terrain with multiple mixed environments blended together", "TerrainV3", true);

        public static readonly Location TerrainGPUFoliageTest = new("GPU Foliage Test",
            "Terrain with lots of foliage relying on the GPU - feedback appreciated!", "TerrainWorkspace", true);

        private Location(string name, string description, string sceneToLoad, bool isTerrain) {
            Id = GenerateId;
            Name = name;
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

        public static IEnumerable<Location> List() {
            return new[] { ProvingGrounds, Space, TestSpaceStation, TerrainV1, TerrainV2, TerrainV3, TerrainGPUFoliageTest };
        }

        public static Location FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static Location FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}