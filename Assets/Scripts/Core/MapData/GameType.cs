using System.Collections.Generic;
using Misc;

namespace Core.MapData {
    public class GameType : IFdEnum {
        private GameType(int id, string name, bool isHotJoinable, bool canWarpToHost) {
            Id = id;
            Name = name;
            IsHotJoinable = isHotJoinable;
            CanWarpToHost = canWarpToHost;
        }

        public static GameType FreeRoam => new(0, "Free Roam", true, true);
        public static GameType TimeTrial => new(1, "Time Trial", false, false);
        public static GameType Sprint => new(2, "Sprint", false, false);
        public static GameType Laps => new(3, "Laps", false, false);
        public static GameType HoonAttack => new(4, "Hoon Attack", false, false);
        public bool IsHotJoinable { get; }
        public bool CanWarpToHost { get; }

        public int Id { get; }
        public string Name { get; }

        public static IEnumerable<GameType> List() {
            return new[] { FreeRoam, TimeTrial, Sprint, Laps, HoonAttack };
        }

        public static GameType FromString(string locationString) {
            return FdEnum.FromString(List(), locationString);
        }

        public static GameType FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}