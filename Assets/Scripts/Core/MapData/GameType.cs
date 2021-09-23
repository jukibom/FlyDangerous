using System.Collections.Generic;
using Misc;

namespace Core.MapData {
    public class GameType : IFdEnum {

        public static GameType FreeRoam => new GameType(0, "Free Roam", true, true);
        public static GameType TimeTrial => new GameType(1, "Time Trial", false, false);
        public static GameType Sprint => new GameType(2, "Sprint", false, false);
        public static GameType Laps => new GameType(3, "Laps", false, false);
        public static GameType HoonAttack => new GameType(4, "Hoon Attack", false, false);
        
        public int Id { get; private set; }
        public string Name { get; private set; }
        public bool IsHotJoinable { get; private set; }
        public bool CanWarpToHost { get; private set; }
        
        private GameType(int id, string name, bool isHotJoinable, bool canWarpToHost) {
            Id = id;
            Name = name;
            IsHotJoinable = isHotJoinable;
            CanWarpToHost = canWarpToHost;
        }

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