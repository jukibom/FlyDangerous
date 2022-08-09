using System.Runtime.InteropServices;

namespace Bhaptics.Tact.Unity
{
    public class HapticApi
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct point
        {
            public float x, y;
            public int intensity, motorCount;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct status
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public int[] values;
        };

        [DllImport("haptic_library")]
        extern public static bool TryGetExePath(byte[] buf, ref int size);

        [DllImport("haptic_library")]
        extern public static void Initialise(string appId, string appName);

        [DllImport("haptic_library")]
        extern public static void Destroy();
        
        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void RegisterFeedback(string str, string projectJson);

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void RegisterFeedbackFromTactFile(string str, string tactFileStr);

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void RegisterFeedbackFromTactFileReflected(string str, string tactFileStr);

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void SubmitRegistered(string key);
        
        // Works with bHaptics Player 1.5.6 onwards
        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void SubmitRegisteredStartMillis(string key, int startTimeMillis);

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void SubmitRegisteredWithOption(string key, string altKey, float intensity, float duration, float offsetX, float offsetY);

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void SubmitByteArray(string key, PositionType pos, byte[] charPtr, int length, int durationMillis);
        
        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void SubmitPathArray(string key, PositionType pos, point[] charPtr, int length, int durationMillis);
        
        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static bool IsFeedbackRegistered(string key);
        
        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static bool IsPlaying();

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static bool IsPlayingKey(string key);

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void TurnOff();
        
        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void TurnOffKey(string key);

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void EnableFeedback();

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void DisableFeedback();

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static void ToggleFeedback();
        
        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static bool IsDevicePlaying(PositionType pos);

        [DllImport("haptic_library", CallingConvention = CallingConvention.Cdecl)]
        extern public static bool TryGetResponseForPosition(PositionType pos, out status status);
    }
}
