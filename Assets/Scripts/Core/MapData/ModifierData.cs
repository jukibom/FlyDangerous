namespace Core.MapData {
    public enum ModifierMode {
        Boost
    }

    public class ModifierData {
        public string Name { get; set; }
        public virtual ModifierMode ModifierMode { get; set; }
    }

    public class BoostModifierData : ModifierData {
        public override ModifierMode ModifierMode => ModifierMode.Boost;
        public float BoostLengthMeters { get; set; }
    }
}