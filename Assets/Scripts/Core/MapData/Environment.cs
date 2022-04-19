using System.Collections.Generic;
using Misc;

namespace Core.MapData {
    public class Environment : IFdEnum {
        private static int _id;

        public static readonly Environment SunriseClear = new("Sunrise Clear", "Sunrise_Clear");
        public static readonly Environment NoonClear = new("Noon Clear", "Noon_Clear");
        public static readonly Environment NoonCloudy = new("Noon Cloudy", "Noon_Cloudy");
        public static readonly Environment NoonStormy = new("Noon Stormy", "Noon_Stormy");
        public static readonly Environment SunsetClear = new("Sunset Clear", "Sunset_Clear");
        public static readonly Environment SunsetCloudy = new("Sunset Cloudy", "Sunset_Cloudy");
        public static readonly Environment NightClear = new("Night Clear", "Night_Clear");
        public static readonly Environment NightCloudy = new("Night Cloudy", "Night_Cloudy");
        public static readonly Environment PlanetOrbitBottom = new("Red Planet", "Planet_Orbit_Bottom");
        public static readonly Environment PlanetOrbitTop = new("Blue Planet", "Planet_Orbit_Top");
        public static readonly Environment RedBlueNebula = new("Red / Blue Nebula", "Red_Blue_Nebula");
        public static readonly Environment YellowGreenNebula = new("Yellow / Green Nebula", "Yellow_Green_Nebula");

        private Environment(string name, string sceneToLoad) {
            Id = GenerateId;
            Name = name;
            SceneToLoad = sceneToLoad;
        }

        private static int GenerateId => _id++;
        public string SceneToLoad { get; }

        public int Id { get; }
        public string Name { get; }

        public static IEnumerable<Environment> List() {
            return new[] {
                SunriseClear, NoonClear, NoonCloudy, NoonStormy, SunsetClear, SunsetCloudy, NightClear, NightCloudy, PlanetOrbitBottom, PlanetOrbitTop,
                RedBlueNebula, YellowGreenNebula
            };
        }

        public static Environment FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static Environment FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}