using UnityEngine;

public class BhapticsLogger
{
    public static LogLevel level = LogLevel.Info;

    public enum LogLevel
    {
        Debug, Info, Error
    }

    public static void LogDebug(string format, params object[] args)
    {
        if (level == LogLevel.Debug)
        {
            Debug.LogFormat("[bhaptics] " + format, args);
        }
    }
    public static void LogInfo(string format, params object[] args)
    {
        if (level == LogLevel.Error)
        {
            return;
        }

        Debug.LogFormat("[bhaptics] " + format, args);
    }
    public static void LogError(string format, params object[] args)
    {
        Debug.LogErrorFormat("[bhaptics] " + format, args);
    }
}
