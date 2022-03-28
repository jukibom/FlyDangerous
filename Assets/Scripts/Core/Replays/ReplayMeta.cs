using Newtonsoft.Json;

namespace Core.Replays {
    public class ReplayMeta {
        public string Version { get; }
        public int KeyFrameIntervalTicks { get; }
        public int KeyFrameBufferSizeBytes { get; }
        public int InputFrameBufferSizeBytes { get; }

        [JsonConstructor]
        private ReplayMeta(string version, int keyFrameIntervalTicks, int keyFrameBufferSizeBytes, int inputFrameBufferSizeBytes) {
            Version = version;
            KeyFrameIntervalTicks = keyFrameIntervalTicks;
            KeyFrameBufferSizeBytes = keyFrameBufferSizeBytes;
            InputFrameBufferSizeBytes = inputFrameBufferSizeBytes;
        }
        
        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static ReplayMeta FromJsonString(string json) {
            return JsonConvert.DeserializeObject<ReplayMeta>(json);
        }

        public static ReplayMeta Version100() {
            return new ReplayMeta("1.0.0", 25, 86, 39);
        }
    }
}