using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Bhaptics.Tact.Unity
{
    public class BhapticsUtils
    {
        private static bool isInit = false;
        private static string exeFilePath = null;

        private static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static string GetExePath()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            
            if (isInit)
            {
                return exeFilePath;
            }
            isInit = true;
            try
            {
                byte[] buf = new byte[500];
                int size = 0;


                if (HapticApi.TryGetExePath(buf, ref size))
                {
                    byte[] cleanedArray = SubArray(buf, 0, size);

                    exeFilePath = System.Text.Encoding.UTF8.GetString(cleanedArray).Trim();
                    return exeFilePath;
                }
            }
            catch (Exception e)
            {
                BhapticsLogger.LogError(e.Message);
            }

            exeFilePath = "";

            return exeFilePath;
            
#else
            return "";
#endif
        }

        public static bool IsPlayerInstalled()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var path = GetExePath();

            return path != null;
#else
            return true;
#endif
        }

        public static bool IsPlayerRunning()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(GetExePath());
                if (Is64BitBuild())
                {
                    // 64 bit machine
                    var processs = System.Diagnostics.Process.GetProcessesByName(fileName);
                    if (processs.Length >= 1)
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    // 32 bit machine does not support GetProcessByName.
                    return true;
                }
            }
            catch (Exception e)
            {
                BhapticsLogger.LogError("IsPlayerRunning() " + e.Message);
            }

            return true;


#else
            return true;
#endif

        }

        private static bool Is64BitBuild()
        {
            return IntPtr.Size == 8;
        }

        public static void LaunchPlayer(bool tryLaunch)
        {
            if (!tryLaunch)
            {
                return;
            }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try
            {
                var myProcess = new System.Diagnostics.Process();
                myProcess.StartInfo.FileName = GetExePath();
                myProcess.Start();
            }
            catch (Exception e)
            {
                BhapticsLogger.LogInfo("LaunchPlayer() " + e.Message);
            }
#endif
        }


        public static float Angle(Vector3 fwd, Vector3 targetDir)
        {
            var fwd2d = new Vector3(fwd.x, 0, fwd.z);
            var targetDir2d = new Vector3(targetDir.x, 0, targetDir.z);

            float angle = Vector3.Angle(fwd2d, targetDir2d);

            if (AngleDir(fwd, targetDir, Vector3.up) == -1)
            {
                angle = 360.0f - angle;
                if (angle > 359.9999f)
                    angle -= 360.0f;
                return angle;
            }

            return angle;
        }

        private static int AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
        {
            Vector3 perp = Vector3.Cross(fwd, targetDir);
            float dir = Vector3.Dot(perp, up);

            if (dir > 0.0)
            {
                return 1;
            }

            if (dir < 0.0)
            {
                return -1;
            }

            return 0;
        }

        public static PositionType ToPositionType(HapticClipPositionType pos)
        {
            switch (pos)
            {
                case HapticClipPositionType.Head:
                    return PositionType.Head;
                case HapticClipPositionType.VestFront:
                    return PositionType.VestFront;
                case HapticClipPositionType.VestBack:
                    return PositionType.VestBack;
                case HapticClipPositionType.LeftHand:
                    return PositionType.HandL;
                case HapticClipPositionType.RightHand:
                    return PositionType.HandR;
                case HapticClipPositionType.LeftFoot:
                    return PositionType.FootL;
                case HapticClipPositionType.RightFoot:
                    return PositionType.FootR;
                case HapticClipPositionType.RightForearm:
                    return PositionType.ForearmR;
                case HapticClipPositionType.LeftForearm:
                    return PositionType.ForearmL;
                case HapticClipPositionType.LeftGlove:
                    return PositionType.GloveL;
                case HapticClipPositionType.RightGlove:
                    return PositionType.GloveR;
            }

            return PositionType.Head;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="isLeft">Value used for devices with left and right sides.(Default: Left)</param>
        /// <returns></returns>
        public static PositionType ToPositionType(HapticDeviceType pos, bool isLeft = true)
        {
            switch (pos)
            {
                case HapticDeviceType.Tactal:
                    return PositionType.Head;
                case HapticDeviceType.TactSuit:
                    return PositionType.Vest;
                case HapticDeviceType.Tactosy_arms:
                    return isLeft ? PositionType.ForearmL : PositionType.ForearmR;
                case HapticDeviceType.Tactosy_feet:
                    return isLeft ? PositionType.FootL : PositionType.FootR;
                case HapticDeviceType.Tactosy_hands:
                    return isLeft ? PositionType.HandL : PositionType.HandR;
                case HapticDeviceType.TactGlove:
                    return isLeft ? PositionType.GloveL : PositionType.GloveR;
            }

            return PositionType.Head;
        }

        public const string TypeHead = "Head";
        public const string TypeTactal = "Tactal";
        public const string TypeVest = "Vest";
        public const string TypeTactot = "Tactot";
        public const string TypeTactosy = "Tactosy";
        public const string TypeTactosy2 = "Tactosy2";
        public const string TypeHand = "Hand";
        public const string TypeFoot = "Foot";
        public const string TypeGlove = "Glove";

#if UNITY_EDITOR
        public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            return assets;
        }
#endif
    }
}

