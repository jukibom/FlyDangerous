using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Core.MapData.Serializable {
    [StructLayout(LayoutKind.Explicit, Size = 12, CharSet = CharSet.Ansi)]
    public class SerializableVector3 {
        [FieldOffset(0)] public float x;
        [FieldOffset(4)] public float y;
        [FieldOffset(8)] public float z;

        public SerializableVector3() {
        }

        public SerializableVector3(Vector3 inVector) {
            x = inVector.x;
            y = inVector.y;
            z = inVector.z;
        }

        public SerializableVector3(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToVector3() {
            return new Vector3(
                x,
                y,
                z
            );
        }

        public SerializableVector3 SetFromVector3(Vector3 inVector) {
            x = inVector.x;
            y = inVector.y;
            z = inVector.z;
            return this;
        }

        public override string ToString() {
            return "[ " +
                   x.ToString(CultureInfo.InvariantCulture) + ", " +
                   y.ToString(CultureInfo.InvariantCulture) + ", " +
                   z.ToString(CultureInfo.InvariantCulture) +
                   " ]";
        }

        public static SerializableVector3 FromVector3(Vector3 value) {
            return new SerializableVector3(value.x, value.y, value.z);
        }

        public static SerializableVector3 AssignOrCreateFromVector3(SerializableVector3 serializableVector3, Vector3 inVector) {
            return serializableVector3 != null ? serializableVector3.SetFromVector3(inVector) : new SerializableVector3(inVector);
        }
    }
}