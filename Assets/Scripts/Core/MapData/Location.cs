#nullable enable
using System.Collections.Generic;
using Misc;

namespace Core.MapData {

    public class Location : IFdEnum {
        // Declare locations here and add to the List() function below
        public static Location Space => new Location(0, "Space", "Space", false);
        public static Location TestSpaceStation => new Location(1, "Space Station", "SpaceStation", false);
        public static Location TerrainV1 => new Location(2, "Flat World", "TerrainV1", true);
        public static Location TerrainV2 => new Location(3, "Canyons", "TerrainV2", true);
        public static Location TerrainV3 => new Location(4, "Biome World", "TerrainV3", true);
    
        
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string SceneToLoad { get; private set; }
        public bool IsTerrain { get; private set; }

        private Location(int id, string name, string sceneToLoad, bool isTerrain) {
            Id = id;
            Name = name;
            SceneToLoad = sceneToLoad; 
            IsTerrain = isTerrain;
        }

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