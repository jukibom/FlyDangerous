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
        private static readonly IGameMode TimeTrialPuzzleGameMode = new TimeTrialPuzzle();
        private static readonly IGameMode HoonAttackGameMode = new HoonAttack();

        public static readonly GameType FreeRoam = new("Free Roam", FreeRoamGameMode);
        public static readonly GameType Sprint = new("Sprint", TimeTrialSprintGameMode);
        public static readonly GameType Laps = new("Laps", TimeTrialLapsGameMode);
        public static readonly GameType Puzzle = new("Puzzle", TimeTrialPuzzleGameMode);
        public static readonly GameType HoonAttack = new("Hoon Attack", HoonAttackGameMode);

        private GameType(string name, IGameMode gameMode) {
            Id = GenerateId;
            Name = name;
            GameMode = gameMode;
        }

        private static int GenerateId => _id++;

        public int Id { get; }
        public string Name { get; }

        public IGameMode GameMode { get; }

        public static IEnumerable<GameType> List() {
            return new[] { FreeRoam, Sprint, Laps, Puzzle, HoonAttack };
        }

        [UsedImplicitly] // see FdEnum.ReadJson
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