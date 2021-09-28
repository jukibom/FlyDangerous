using System.Collections.Generic;
using Misc;

namespace Core.Ship {
    public class ShipMeta : IFdEnum {

        public static ShipMeta Puffin => new ShipMeta(0, "Puffin", "OG-Puffin", "Puffin",
            "Some say it's a prototype for the FD1. Others say it's a retrofitted brick with ridiculous indicators. Either way, it flies!");

        public static ShipMeta Calidris => new ShipMeta(1, "Calidris", "FD1-Calidris", "Calidris",
            "A sleek, long-nosed racer, the Calidris has enormous rear thrusters with enough oomph to catapult you at obnoxious speed for it's size. Some say it was the first to include ancient \"scoop-based velocity limiting\" as standard. Whatever that means.");
        
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        private string _prefabToLoad;
        public string PrefabToLoad {
            get => $"Ships/{_prefabToLoad}";
            private set => _prefabToLoad = value;
        }
        public string Description { get; private set; }

        private ShipMeta(int id, string name, string fullName, string prefabToLoad, string description) {
            Id = id;
            Name = name;
            FullName = fullName;
            PrefabToLoad = prefabToLoad;
            Description = description;
        }
        
        public static IEnumerable<ShipMeta> List() {
            return new[] { Puffin, Calidris };
        }

        public static ShipMeta FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static ShipMeta FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}