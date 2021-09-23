using System.Collections.Generic;
using Misc;

namespace Core.MapData {
    public class Environment : IFdEnum {
        public static Environment PlanetOrbitBottom => new Environment(0, "Planet Orbit (Top)", "Planet_Orbit_Bottom");
        public static Environment PlanetOrbitTop => new Environment(1, "Planet Orbit (Bottom)", "Planet_Orbit_Top");
        public static Environment SunriseClear => new Environment(2, "Sunrise Clear", "Sunrise_Clear");
        public static Environment NoonClear => new Environment(3, "Noon Clear", "Noon_Clear");
        public static Environment NoonCloudy => new Environment(4, "Noon Cloudy", "Noon_Cloudy");
        public static Environment NoonStormy => new Environment(5, "Noon Stormy", "Noon_Stormy");
        public static Environment SunsetClear => new Environment(6, "Sunset Clear", "Sunset_Clear");
        public static Environment SunsetCloudy => new Environment(7, "Sunset Cloudy", "Sunset_Cloudy");
        public static Environment NightClear => new Environment(8, "Night Clear", "Night_Clear");
        public static Environment NightCloudy => new Environment(9, "Night Cloudy", "Night_Cloudy");
        
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string SceneToLoad { get; private set; }
        
        private Environment(int id, string name, string sceneToLoad) {
            Id = id;
            Name = name;
            SceneToLoad = sceneToLoad;
        }

        public static IEnumerable<Environment> List() {
            return new[] { PlanetOrbitBottom, PlanetOrbitTop, SunriseClear, NoonClear, NoonCloudy, NoonStormy, SunsetClear, SunsetCloudy, NightClear, NightCloudy };
        }

        public static Environment FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static Environment FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}