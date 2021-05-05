using System.Collections.Generic;
using JetBrains.Annotations;

namespace Engine {
    public class LevelDataVector2<T> {
        public T x;
        public T y;
    }
    
    public class LevelDataVector3<T> {
        public T x;
        public T y;
        public T z;
    }

    public enum Location {
        TestSpaceStation,
        Terrain,
    }
    
    public enum RaceType {
        Sprint,
        Laps,
        FreeRoam,
    }

    public class Checkpoint {
        public LevelDataVector3<float> position;
        public LevelDataVector3<float> rotation;
    }
    
    public class LevelData {
        public Location location;
        public string terrainSeed;
        public LevelDataVector2<int> terrainTile = new LevelDataVector2<int>();
        public LevelDataVector3<float> startPosition = new LevelDataVector3<float>();
        public LevelDataVector3<float> startRotation = new LevelDataVector3<float>();
        public RaceType raceType;
        [CanBeNull] public Checkpoint start;
        [CanBeNull] public List<Checkpoint> checkpoints;
        [CanBeNull] public Checkpoint end;
    }
}