using System.Collections.Generic;
using Misc;

namespace Core.MapData {
    public class GameType : IFdEnum {
        private static int _id;

        public static readonly GameType FreeRoam = new("Free Roam", true, true, false);
        public static readonly GameType TimeTrial = new("Time Trial", false, false, true);
        public static readonly GameType Sprint = new("Sprint", false, false, true);
        public static readonly GameType Laps = new("Laps", false, false, true);
        public static readonly GameType HoonAttack = new("Hoon Attack", false, false, false);
        public static readonly GameType Training = new("Training", false, false, true);

        private GameType(string name, bool isHotJoinable, bool canWarpToHost, bool hasFixedStartLocation) {
            Id = GenerateId;
            Name = name;
            IsHotJoinable = isHotJoinable;
            CanWarpToHost = canWarpToHost;
            HasFixedStartLocation = hasFixedStartLocation;
        }

        private static int GenerateId => _id++;
        public bool IsHotJoinable { get; }
        public bool CanWarpToHost { get; }
        public bool HasFixedStartLocation { get; }

        public int Id { get; }
        public string Name { get; }

        public static IEnumerable<GameType> List() {
            return new[] { FreeRoam, TimeTrial, Sprint, Laps, HoonAttack,Training };
        }

        public static GameType FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static GameType FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}