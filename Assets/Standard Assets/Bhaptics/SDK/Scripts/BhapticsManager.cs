using Bhaptics.Tact.Unity;
using UnityEngine;

public class BhapticsManager
{
    private static IHaptic Haptic;

    public static bool Init = false;


    public static IHaptic GetHaptic()
    {
        if (Haptic == null)
        {
            try
            {
                Init = true;
                if (Application.platform == RuntimePlatform.Android)
                {
                    Haptic = new AndroidHaptic();
                    BhapticsLogger.LogInfo("Android initialized.");
                }
                else
                {
                    Haptic = new BhapticsHaptic();
                    BhapticsLogger.LogInfo("Initialized.");
                }
            }
            catch (System.Exception e)
            {
                BhapticsLogger.LogError("BhapticsManager.cs - GetHaptic() / " + e.Message);
            }
        }

        return Haptic;
    }

    public static void Initialize()
    {
        GetHaptic();
    }

    public static void Dispose()
    {
        if (Haptic != null)
        {
            Init = false;
            Haptic.TurnOff();
            BhapticsLogger.LogInfo("Dispose() bHaptics plugin.");
            Haptic.Dispose();
            Haptic = null;
        }
    }
}
