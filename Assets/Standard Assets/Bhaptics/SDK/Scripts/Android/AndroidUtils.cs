using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bhaptics.Tact.Unity
{
    [Serializable]
    public class HapticDevice
    {
        public bool IsPaired;
        public bool IsConnected;
        public string DeviceName;
        public PositionType Position;
        public string Address;
        public PositionType[] Candidates;
        public bool IsEnable;
        public bool IsAudioJack;
        public int Battery;
    }

    public static class AndroidUtils
    {
        [Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }


        [Serializable]
        private class Device
        {
            public bool paired;
            public string deviceName;
            public int position;
            public bool connected;
            public string address;
            public int battery;
            public bool enable;
            public bool audioJackIn;
        }
        [Serializable]
        public class StreamHost
        {
            public string ip;
            public bool connected;
        }

        private static PositionType ToDeviceType(int type)
        {
            switch (type)
            {
                case 3:
                    return PositionType.Head;
                case 0:
                    return PositionType.Vest;
                case 1:
                    return PositionType.ForearmL;
                case 2:
                    return PositionType.ForearmR;
                case 4:
                    return PositionType.HandL;
                case 5:
                    return PositionType.HandR;
                case 6:
                    return PositionType.FootL;
                case 7:
                    return PositionType.FootR;
                case 8:
                    return PositionType.GloveL;
                case 9:
                    return PositionType.GloveR;

            }

            return PositionType.Vest;
        }

        private static PositionType[] ToCandidates(int type)
        {
            switch (type)
            {
                case 3:
                    return new PositionType[] { PositionType.Head };
                case 0:
                    return new PositionType[] { PositionType.Vest };
                case 1:
                    return new PositionType[] { PositionType.ForearmL, PositionType.ForearmR };
                case 2:
                    return new PositionType[] { PositionType.ForearmL, PositionType.ForearmR };
                case 4:
                    return new PositionType[] { PositionType.HandL, PositionType.HandR };
                case 5:
                    return new PositionType[] { PositionType.HandL, PositionType.HandR };
                case 6:
                    return new PositionType[] { PositionType.FootR, PositionType.FootL };
                case 7:
                    return new PositionType[] { PositionType.FootR, PositionType.FootL };
                case 8:
                    return new PositionType[] { PositionType.GloveL};
                case 9:
                    return new PositionType[] { PositionType.GloveL};

            }

            return new PositionType[0];
        }

        public static bool IsLeft(PositionType pos)
        {
            return pos == PositionType.ForearmL
                   || pos == PositionType.FootL
                   || pos == PositionType.HandL;
        }

        public static bool CanChangePosition(PositionType pos)
        {
            return !(pos == PositionType.Head || pos == PositionType.Vest);
        }



        // https://answers.unity.com/questions/1123326/jsonutility-array-not-supported.html
        public static T[] GetJsonArray<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }


        private static HapticDevice Convert(Device d)
        {
            var isConnected = d.connected;
            return new HapticDevice()
            {
                IsPaired = d.paired,
                IsConnected = isConnected,
                Address = d.address,
                Position = ToDeviceType(d.position),
                DeviceName = d.deviceName,
                Candidates = ToCandidates(d.position),
                Battery = d.battery,
                IsAudioJack = d.audioJackIn,
                IsEnable = d.enable
            };
        }

        public static List<HapticDevice> ConvertToBhapticsDevices(string[] deviceJson)
        {
            var res = new List<HapticDevice>();

            for (var i = 0; i < deviceJson.Length; i++)
            {
                var device = JsonUtility.FromJson<Device>(deviceJson[i]);
                res.Add(Convert(device));
            }

            return res;
        }


        public static void CallNativeVoidMethod(IntPtr androidObjPtr, IntPtr methodPtr, object[] param)
        {
            jvalue[] args = AndroidJNIHelper.CreateJNIArgArray(param);
            try
            {
                AndroidJNI.CallVoidMethod(androidObjPtr, methodPtr, args);
            }
            catch (Exception e)
            {
                BhapticsLogger.LogError("CallNativeVoidMethod() : {0}", e.Message);
            }
            finally
            {
                AndroidJNIHelper.DeleteJNIArgArray(param, args);
            }
        }


        public static bool CallNativeBoolMethod(IntPtr androidObjPtr, IntPtr methodPtr, object[] param)
        {
            jvalue[] args = AndroidJNIHelper.CreateJNIArgArray(param);
            bool res = false;
            try
            {
                res = AndroidJNI.CallBooleanMethod(androidObjPtr, methodPtr, args);
            }
            catch (Exception e)
            {
                BhapticsLogger.LogError("CallNativeBoolMethod() : {0}", e.Message);
            }
            finally
            {
                AndroidJNIHelper.DeleteJNIArgArray(param, args);
            }

            return res;
        }
    }

}