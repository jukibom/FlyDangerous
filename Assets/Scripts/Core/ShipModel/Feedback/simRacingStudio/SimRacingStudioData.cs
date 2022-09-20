using System;
using System.Runtime.InteropServices;

namespace Core.ShipModel.Feedback.simRacingStudio {
    [StructLayout(LayoutKind.Sequential)]
    public struct SimRacingStudioData {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public char[] apiMode;

        public uint version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public char[] game;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public char[] vehicleName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public char[] location;

        public float speed;
        public float rpm;
        public float maxRpm;
        public int gear;
        public float pitch;
        public float roll;
        public float yaw;
        public float lateralVelocity;
        public float lateralAcceleration;
        public float verticalAcceleration;
        public float longitudinalAcceleration;
        public float suspensionTravelFrontLeft;
        public float suspensionTravelFrontRight;
        public float suspensionTravelRearLeft;
        public float suspensionTravelRearRight;
        public uint wheelTerrainFrontLeft;
        public uint wheelTerrainFrontRight;
        public uint wheelTerrainRearLeft;
        public uint wheelTerrainRearRight;
    }

    public static class SimRacingStudioDataEncoder {
        // used for pre-allocating bytes for encoding (size of struct never changes)
        private static int _sizeBytes;
        private static byte[] _arrBytes;

        internal static byte[] EncodePacket(SimRacingStudioData telemetryStruct) {
            if (_arrBytes == null) {
                _sizeBytes = Marshal.SizeOf(telemetryStruct);
                _arrBytes = new byte[_sizeBytes];
            }

            var ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(_sizeBytes);
                Marshal.StructureToPtr(telemetryStruct, ptr, false);
                Marshal.Copy(ptr, _arrBytes, 0, _sizeBytes);
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }

            return _arrBytes;
        }

        // SRS expects angles as -180 to 180, unity outputs as 0-360.
        internal static float MapAngleToSrs(float angleDegrees) {
            return angleDegrees > 180 ? angleDegrees - 360 : angleDegrees;
        }
    }
}