using Gameplay;

namespace Core.MapData.Serializable {
    public class SerializableCheckpoint {
        public SerializableVector3 position;
        public SerializableVector3 rotation;
        public CheckpointType type;

        public static SerializableCheckpoint FromCheckpoint(Checkpoint checkpoint) {
            var checkpointLocation = new SerializableCheckpoint();
            var transform = checkpoint.transform;
            checkpointLocation.position = SerializableVector3.FromVector3(transform.localPosition);
            checkpointLocation.rotation = SerializableVector3.FromVector3(transform.rotation.eulerAngles);
            checkpointLocation.type = checkpoint.Type;
            return checkpointLocation;
        }

        // convert this object into a string for hash generation purposes (that is, any information pertinent to the level format for competition purposes)
        public static string ToHashString(SerializableCheckpoint checkpoint) {
            return checkpoint.position.ToString() + checkpoint.rotation + checkpoint.type;
        }
    }
}