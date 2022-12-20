using System.Collections.Generic;
using Gameplay.Game_Modes;
using Gameplay.Game_Modes.Components.Interfaces;
using JetBrains.Annotations;
using Misc;

namespace Core.MapData {
    public class GameType : IFdEnum {
        private static int _id;

        private static readonly IGameMode FreeRoamGameMode = new FreeRoam();
        private static readonly IGameMode TimeTrialSprintGameMode = new TimeTrialSprint();
        private static readonly IGameMode TimeTrialLapsGameMode = new TimeTrialLaps();
        private static readonly IGameMode HoonAttackGameMode = new HoonAttack();

        public static readonly GameType FreeRoam = new("Free Roam", FreeRoamGameMode, true, true, false);
        public static readonly GameType Sprint = new("Sprint", TimeTrialSprintGameMode, false, false, true);
        public static readonly GameType Laps = new("Laps", TimeTrialLapsGameMode, false, false, true);
        public static readonly GameType HoonAttack = new("Hoon Attack", HoonAttackGameMode, false, false, false);

        private GameType(string name, IGameMode gameMode, bool isHotJoinable, bool canWarpToHost, bool hasFixedStartLocation) {
            Id = GenerateId;
            Name = name;
            GameMode = gameMode;
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

        public IGameMode GameMode { get; }

        public static IEnumerable<GameType> List() {
            return new[] { FreeRoam, Sprint, Laps, HoonAttack };
        }

        [UsedImplicitly] // FdEnum.ReadJson
        public static GameType FromString(string gameTypeString) {
            // handler for old meta data
            if (gameTypeString == "Time Trial") gameTypeString = "Sprint";

            return FdEnum.FromString(List(), gameTypeString);
        }

        public static GameType FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}