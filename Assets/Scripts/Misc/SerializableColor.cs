using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Misc {
    [StructLayout(LayoutKind.Explicit, Size = 12, CharSet = CharSet.Ansi)]
    public class SerializableColor32 {
        [FieldOffset(0)] public uint r;
        [FieldOffset(4)] public uint g;
        [FieldOffset(8)] public uint b;

        public SerializableColor32() {
        }

        public SerializableColor32(Color32 inColor) {
            r = inColor.r;
            g = inColor.g;
            b = inColor.b;
        }

        public SerializableColor32(Color inColor) {
            r = (uint)(inColor.r * 255);
            g = (uint)(inColor.g * 255);
            b = (uint)(inColor.b * 255);
        }

        public SerializableColor32(uint r, uint g, uint b) {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public Color ToColor() {
            return new Color(
                (float)r / 255,
                (float)g / 255,
                (float)b / 255
            );
        }

        public SerializableColor32 SetFromColor(Color inColor) {
            r = (uint)(inColor.r * 255);
            g = (uint)(inColor.g * 255);
            b = (uint)(inColor.b * 255);
            return this;
        }

        public override string ToString() {
            return "[ " +
                   r.ToString(CultureInfo.InvariantCulture) + ", " +
                   g.ToString(CultureInfo.InvariantCulture) + ", " +
                   b.ToString(CultureInfo.InvariantCulture) +
                   " ]";
        }

        public static SerializableColor32 FromColor(Color value) {
            return new SerializableColor32().SetFromColor(value);
        }

        public static SerializableColor32 AssignOrCreateFromColor(SerializableColor32 serializableColor32, Color inColor) {
            return serializableColor32 != null ? serializableColor32.SetFromColor(inColor) : new SerializableColor32(inColor);
        }
    }
}