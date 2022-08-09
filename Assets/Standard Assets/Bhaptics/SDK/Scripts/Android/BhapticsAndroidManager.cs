using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Bhaptics.Tact.Unity
{ 
    public class BhapticsAndroidManager : MonoBehaviour
    {
        private static BhapticsAndroidManager Instance;


        public static bool pcAndoidTestMode = false;
        
        private List<HapticDevice> Devices = new List<HapticDevice>();

        private static List<UnityAction> refreshActions = new List<UnityAction>();

        void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }

            Instance = this;
            name = "[bHapticsAndroidManager]";
        }

        void Start()
        {
#if UNITY_ANDROID
            if (Application.platform != RuntimePlatform.Android)
            {
                pcAndoidTestMode = true;
            }

            InvokeRepeating("RefreshDevices", 1f, 1f);

#endif
        }

        private void RefreshDevices()
        {
            if (refreshActions.Count == 0)
            {
                return;
            }

            var androidHapticPlayer = BhapticsManager.GetHaptic() as AndroidHaptic;
            if (androidHapticPlayer == null)
            {
                if (Devices.Count == 0)
                {
                    var device = new HapticDevice()
                    {
                        Position = PositionType.Vest,
                        IsConnected = true,
                        IsPaired = true,
                        Address = "aaaa",
                        DeviceName = "Tactot",
                        Candidates = new PositionType[] { PositionType.Vest },
                    };
                    var device2 = new HapticDevice()
                    {
                        Position = PositionType.ForearmL,
                        IsConnected = false,
                        IsPaired = true,
                        Address = "aaaa22",
                        DeviceName = "Tactosy",
                        Candidates = new PositionType[] { PositionType.ForearmR, PositionType.ForearmL },
                    };
                    var device3 = new HapticDevice()
                    {
                        Position = PositionType.HandL,
                        IsConnected = true,
                        IsPaired = true,
                        Address = "aaaa22",
                        DeviceName = "Tactosy",
                        Candidates = new PositionType[] { PositionType.HandL, PositionType.HandR},
                    };
                    var device4 = new HapticDevice()
                    {
                        Position = PositionType.Head,
                        IsConnected = true,
                        IsPaired = true,
                        Address = "aaaa22",
                        DeviceName = "Tactal",
                        Candidates = new PositionType[] { PositionType.Head},
                    };

                    var device5 = new HapticDevice()
                    {
                        Position = PositionType.Vest,
                        IsConnected = true,
                        IsPaired = true,
                        IsAudioJack = true,
                        Address = "aaaa",
                        DeviceName = "Tactot",
                        Battery = 10,
                        Candidates = new PositionType[] { PositionType.Vest },
                    };
                    //Devices.Add(device);
                    //Devices.Add(device2);
                    //Devices.Add(device3);
                    //Devices.Add(device4);
                    //Devices.Add(device5);

                }
                // TODO DEBUGGING USAGE.
                for (var i = 0; i < refreshActions.Count; i++)
                {
                    refreshActions[i].Invoke();
                }
                return;
            }

            Devices = androidHapticPlayer.GetDevices();
            for (var i = 0; i < refreshActions.Count; i++)
            {
                refreshActions[i].Invoke();
            }
        }

        public static void Ping(PositionType pos)
        {
            Debug.LogFormat("PING  ...");
            var connectedDevices = GetConnectedDevices(pos);
            foreach (var pairedDevice in connectedDevices)
            {
                Ping(pairedDevice);
            }
        }

        #region Connection Related Functions
        public static void TogglePosition(string address)
        {
            var androidHapticPlayer = BhapticsManager.GetHaptic() as AndroidHaptic;
            if (androidHapticPlayer == null)
            {
                return;
            }

            androidHapticPlayer.TogglePosition(address);
        }

        public static void Ping(HapticDevice device)
        {
            var androidHapticPlayer = BhapticsManager.GetHaptic() as AndroidHaptic;
            if (androidHapticPlayer == null)
            {
                return;
            }

            Debug.LogFormat("PING  ..." + device.Address);

            androidHapticPlayer.Ping(device.Address);
        }

        public static void PingAll()
        {
            var androidHapticPlayer = BhapticsManager.GetHaptic() as AndroidHaptic;
            if (androidHapticPlayer == null)
            {
                return;
            }

            androidHapticPlayer.PingAll();
        }

        public static List<HapticDevice> GetDevices()
        {
            if (Instance != null)
            {
                return Instance.Devices;
            }


            return new List<HapticDevice>();
        }

        public static List<HapticDevice> GetConnectedDevices(PositionType pos)
        {
            var pairedDeviceList = new List<HapticDevice>();
            var devices = GetDevices();
            foreach (var device in devices)
            {
                if (device.IsPaired && device.Position == pos && device.IsConnected)
                {
                    pairedDeviceList.Add(device);
                }
            }

            return pairedDeviceList;
        }

        public static List<HapticDevice> GetPairedDevices(PositionType pos)
        {
            var res = new List<HapticDevice>();
            var devices = GetDevices();
            foreach (var device in devices)
            {
                if (device.IsPaired && device.Position == pos)
                {
                    res.Add(device);
                }
            }

            return res;
        }

        public static void AddRefreshAction(UnityAction action)
        {
            if (!refreshActions.Contains(action))
            {
                refreshActions.Add(action);
            }
        }

        public static void RemoveRefreshAction(UnityAction action)
        {
            if (refreshActions.Contains(action))
            {
                refreshActions.Remove(action);
            }
        }

        public static void ClearRefreshAction()
        {
            refreshActions.Clear();
        }


        public static bool IsStreaming()
        {

            var androidHapticPlayer = BhapticsManager.GetHaptic() as AndroidHaptic;
            if (androidHapticPlayer == null)
            {
                return false;
            }

            return androidHapticPlayer.IsStreamingEnable();
        }

        public static void ToggleStreaming()
        {
            var androidHapticPlayer = BhapticsManager.GetHaptic() as AndroidHaptic;
            if (androidHapticPlayer == null)
            {
                return;
            }

            androidHapticPlayer.ToggleStreaming();
        }

        public static List<AndroidUtils.StreamHost> GetStreamingHosts()
        {

            var androidHapticPlayer = BhapticsManager.GetHaptic() as AndroidHaptic;
            if (androidHapticPlayer == null)
            {
                return new List<AndroidUtils.StreamHost>();
            }

            return androidHapticPlayer.GetStreamingHosts();
        }

        #endregion

        public static void ShowBluetoothSetting()
        {
            var androidHapticPlayer = BhapticsManager.GetHaptic() as AndroidHaptic;
            if (androidHapticPlayer == null)
            {
                return;
            }

            androidHapticPlayer.ShowBluetoothSetting();
        }
        public static void ToggleEnableDevice(HapticDevice device)
        {
            var androidHapticPlayer = BhapticsManager.GetHaptic() as AndroidHaptic;
            if (androidHapticPlayer == null)
            {
                return;
            }

            androidHapticPlayer.EnableDevice(device.Address, !device.IsEnable);
        }

        void OnApplicationFocus(bool pauseStatus)
        {
            if (pauseStatus)
            {
                var androidHapticPlayer = BhapticsManager.GetHaptic() as AndroidHaptic;
                if (androidHapticPlayer == null)
                {
                    return;
                }

                androidHapticPlayer.RefreshPairingInfo();
            }
        }
    }
}