using System;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace Core.ShipModel.Feedback.socket {
    internal enum BroadcastFormat {
        Bytes,
        Json
    }

    public struct FlyDangerousTelemetry {
        public uint packetId;
        public string version;
        public string currentTrackName;
        public string currentGameMode;
        public float velocity;

        public override string ToString() {
            return $"Fly Dangerous version {version}, packetId: {packetId}, ship velocity: {velocity}m/s";
        }
    }

    #region Serialisers

    public static class FlyDangerousTelemetryEncoder {
        internal static byte[] EncodePacket(BroadcastFormat broadcastFormat, FlyDangerousTelemetry telemetry) {
            byte[] packet;
            switch (broadcastFormat) {
                case BroadcastFormat.Bytes:
                    packet = StructureToByteArray(telemetry);
                    break;
                case BroadcastFormat.Json:
                    packet = StructureToJson(telemetry);
                    break;
                default:
                    throw new Exception("Failed attempting to output telemetry without target format!");
            }

            return packet;
        }

        private static byte[] StructureToJson(FlyDangerousTelemetry telemetryStruct) {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(telemetryStruct, Formatting.Indented));
        }

        // TODO: can we pre-alloc and update the same byte array?
        private static byte[] StructureToByteArray(FlyDangerousTelemetry telemetryStruct) {
            var size = Marshal.SizeOf(telemetryStruct);
            var arr = new byte[size];
            var ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(telemetryStruct, ptr, false);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }

            return arr;
        }
    }

    #endregion

    #region Deserialisers

    public static class FlyDangerousTelemetryDecoder {
        private static FlyDangerousTelemetry _telemetry;
        
        internal static FlyDangerousTelemetry DecodePacket(BroadcastFormat broadcastFormat, byte[] bytes) {
            switch (broadcastFormat) {
                case BroadcastFormat.Bytes:
                    _telemetry = ByteArrayToStructure(bytes);
                    break;
                case BroadcastFormat.Json:
                    _telemetry = JsonStringToStructure(Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                    break;
            }

            return _telemetry;
        }

        private static FlyDangerousTelemetry JsonStringToStructure(string json) {
            return JsonConvert.DeserializeObject<FlyDangerousTelemetry>(json);
        }

        private static FlyDangerousTelemetry ByteArrayToStructure(byte[] bytes) {
            var telemetry = new FlyDangerousTelemetry();

            var size = Marshal.SizeOf(telemetry);
            var ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, ptr, size);
                telemetry = (FlyDangerousTelemetry)Marshal.PtrToStructure(ptr, telemetry.GetType());
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }

            return telemetry;
        }
    }

    #endregion
}