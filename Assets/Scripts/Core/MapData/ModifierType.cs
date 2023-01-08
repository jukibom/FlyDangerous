using System.Collections.Generic;
using Misc;

namespace Core.MapData {
    public class ModifierType : IFdEnum {
        private static int _id;

        public static readonly ModifierData BoostModifierData = new BoostModifierData { Name = "Boost", BoostLengthMeters = 300 };

        public static readonly ModifierType BoostModifierType = new(BoostModifierData);

        private ModifierType(ModifierData modifierData) {
            Id = GenerateId;
            ModifierData = modifierData;
        }

        private static int GenerateId => _id++;
        public int Id { get; }
        public string Name => ModifierData.Name;
        public ModifierData ModifierData { get; }

        public static IEnumerable<ModifierType> List() {
            return new[] {
                BoostModifierType
            };
        }

        public static ModifierType FromString(string modifierName) {
            return FdEnum.FromString(List(), modifierName);
        }

        public static ModifierType FromId(int id) {
            return FdEnum.FromId(List(), id);
        }
    }
}