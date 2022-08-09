using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Bhaptics.Tact.Unity
{
    [Serializable]
    public class PositonIconSetting
    {
        public Sprite connect;
        public Sprite disconnect;
    }

    [Serializable]
    public class IconSetting
    {

        [Header("[Setting Icons]")]
        public PositonIconSetting Vest;
        public PositonIconSetting Head;
        public PositonIconSetting Arm;
        public PositonIconSetting Foot;
        public PositonIconSetting Hand;
        public PositonIconSetting GloveL;
        public PositonIconSetting GloveR;
    }

    public class Android_DeviceController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;

        [SerializeField] private IconSetting widgetSetting;
        [SerializeField] private Sprite TactsuitWiredIcon;
        [SerializeField] private Image batteryLowImage;

        [Header("Connect Menu")]
        [SerializeField] private GameObject ConnectMenu;
        [SerializeField] private Button pingButton;
        [SerializeField] private Button lButton;
        [SerializeField] private Button rButton;
        [SerializeField] private GameObject wiredNotification;

        [Header("Disconnect Menu")] 
        [SerializeField] private GameObject DisconnectMenu;




        private static string SelectHexColor = "#5267F9FF";
        private static string SelectHoverHexColor = "#697CFFFF";
        private static string DisableHexColor = "#525466FF";
        private static string DisableHoverHexColor = "#63646FFF";







        private HapticDevice device;

        void Awake()
        {
            if (pingButton != null)
            {
                pingButton.onClick.AddListener(Ping);
            }

            if (lButton != null)
            {
                lButton.onClick.AddListener(ToLeft);
            }
            
            if (rButton != null)
            {
                rButton.onClick.AddListener(ToRight);
            }
        }


        public void RefreshDevice(HapticDevice d)
        {
            device = d;

            if (device == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            UpdateIcon(d);

            if (d.IsConnected)
            {
                RenderConnectMenu();
            }
            else
            {
                RenderDisconnectMenu();
            }
        }

        private void RenderConnectMenu()
        {
            ConnectMenu.gameObject.SetActive(true);
            DisconnectMenu.gameObject.SetActive(false);
            batteryLowImage.gameObject.SetActive(device.Battery < 20 && device.Battery >= 0);
            UpdateButtons();
        }

        private void RenderDisconnectMenu()
        {
            ConnectMenu.gameObject.SetActive(false);
            DisconnectMenu.gameObject.SetActive(true);
            batteryLowImage.gameObject.SetActive(false);
        }

        private void UpdateButtons()
        {
            if (device.IsAudioJack)
            {
                wiredNotification.SetActive(true);
                pingButton.gameObject.SetActive(false);
                rButton.gameObject.SetActive(false);
                lButton.gameObject.SetActive(false);
                return;
            }

            wiredNotification.SetActive(false);

            if (IsLeft(device.Position) || IsRight(device.Position))
            {
                pingButton.gameObject.SetActive(false);
                lButton.gameObject.SetActive(true);
                rButton.gameObject.SetActive(true);

                var isLeft = IsLeft(device.Position);
                ChangeButtonColor(lButton, isLeft);
                ChangeButtonColor(rButton, !isLeft);
            }
            else
            {
                pingButton.gameObject.SetActive(true);
                lButton.gameObject.SetActive(false);
                rButton.gameObject.SetActive(false);

                ChangeButtonColor(pingButton, true);
            }
        }

        private void UpdateIcon(HapticDevice d)
        {
            switch (d.Position)
            {
                case PositionType.Vest:
                    if (d.IsAudioJack)
                    {
                        icon.sprite = TactsuitWiredIcon;
                        return;
                    }

                    icon.sprite = GetSprite(widgetSetting.Vest, d.IsConnected);
                    break;
                case PositionType.FootL:
                case PositionType.FootR:
                    icon.sprite = GetSprite(widgetSetting.Foot, d.IsConnected);
                    break;
                case PositionType.HandL:
                case PositionType.HandR:
                    icon.sprite = GetSprite(widgetSetting.Hand, d.IsConnected);
                    break;
                case PositionType.ForearmL:
                case PositionType.ForearmR:
                    icon.sprite = GetSprite(widgetSetting.Arm, d.IsConnected);
                    break;
                case PositionType.GloveL:
                    icon.sprite = GetSprite(widgetSetting.GloveL, d.IsConnected);
                    break;
                case PositionType.GloveR:
                    icon.sprite = GetSprite(widgetSetting.GloveR, d.IsConnected);
                    break;
                case PositionType.Head:
                    icon.sprite = GetSprite(widgetSetting.Head, d.IsConnected);
                    break;

                default:
                    icon.sprite = null;
                    break;
            }
        }

        private Sprite GetSprite(PositonIconSetting icon, bool connected)
        {
            if (icon == null)
            {
                return null;
            }

            return connected ? icon.connect : icon.disconnect;
        }

        private void Ping()
        {
            if (device == null)
            {
                return;
            }

            BhapticsAndroidManager.Ping(device);
        }

        private void ToLeft()
        {
            if (device == null)
            {
                return;
            }

            if (IsRight(device.Position))
            {
                BhapticsAndroidManager.TogglePosition(device.Address);
            }
            else
            {
                Ping();
            }
        }

        private void ToRight()
        {
            if (device == null)
            {
                return;
            }

            if (IsLeft(device.Position))
            {
                BhapticsAndroidManager.TogglePosition(device.Address);
            }
            else
            {
                Ping();
            }
        }

        private Color ToColor(string hex)
        {
            Color res = Color.white;

            if (ColorUtility.TryParseHtmlString(hex, out res))
            {
                return res;
            }

            return res;
        }

        private void ChangeButtonColor(Button targetButton, bool isSelect)
        {
            var defaultColor = ToColor(isSelect ? SelectHexColor : DisableHexColor);
            var hoverColor = ToColor(isSelect ? SelectHoverHexColor : DisableHoverHexColor);

            var buttonColors = targetButton.colors;
            buttonColors.normalColor = defaultColor;
            buttonColors.highlightedColor = hoverColor;
            buttonColors.pressedColor = defaultColor;
            targetButton.colors = buttonColors;
        }

        private static bool IsLeft(PositionType pos)
        {
            switch (pos)
            {
                case PositionType.FootL:
                case PositionType.HandL:
                case PositionType.ForearmL:
                case PositionType.Left:
                    return true;

            }
            return false;
        }

        private static bool IsRight(PositionType pos)
        {
            switch (pos)
            {
                case PositionType.FootR:
                case PositionType.HandR:
                case PositionType.ForearmR:
                case PositionType.Right:
                    return true;

            }
            return false;
        }
    }

}