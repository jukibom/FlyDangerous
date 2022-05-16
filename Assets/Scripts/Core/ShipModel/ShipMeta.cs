using System.Collections.Generic;
using Misc;

namespace Core.ShipModel {
    public class ShipMeta : IFdEnum {
        private static int _id;
        private string _prefabToLoad;

        private ShipMeta(string name, string fullName, string prefabToLoad, string description) {
            Id = GenerateId;
            Name = name;
            FullName = fullName;
            PrefabToLoad = prefabToLoad;
            Description = description;
        }

        private static int GenerateId => _id++;

        public static readonly ShipMeta Puffin = new( "Puffin", "OG-Puffin", "Puffin",
            "Some say it's a prototype for the FD1. Others say it's a retrofitted brick with ridiculous indicators. Either way, it flies!");

        public static readonly ShipMeta Calidris = new("Calidris", "FD1-Calidris", "Calidris",
            "A sleek, long-nosed racer, the Calidris has enormous rear thrusters with enough oomph to catapult you at obnoxious speed for its size. Some say it was the first to include ancient \"scoop-based velocity limiting\" as standard. Whatever that means.");

        public string FullName { get; }

        public string PrefabToLoad {
            get => $"Ships/{_prefabToLoad}";
            private set => _prefabToLoad = value;
        }

        public string Description { get; }

        public int Id { get; }
        public string Name { get; }

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