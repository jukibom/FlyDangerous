using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Player {
    public class ShipProfile {
        public readonly string accentColor;
        public readonly string headLightsColor;
        public readonly string playerFlagFilename;
        public readonly string playerName;
        public readonly string primaryColor;
        public readonly string shipModel;
        public readonly string thrusterColor;
        public readonly string trailColor;

        public ShipProfile(string playerName, string playerFlagFilename, string shipModel, string primaryColor, string accentColor, string thrusterColor,
            string trailColor, string headLightsColor) {
            this.playerName = playerName;
            this.playerFlagFilename = playerFlagFilename;
            this.shipModel = shipModel;
            this.primaryColor = primaryColor;
            this.accentColor = accentColor;
            this.thrusterColor = thrusterColor;
            this.trailColor = trailColor;
            this.headLightsColor = headLightsColor;
        }

        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [CanBeNull]
        public static ShipProfile FromJsonString(string json) {
            try {
                return JsonConvert.DeserializeObject<ShipProfile>(json);
            }
            catch (Exception e) {
                Debug.LogWarning(e.Message);
                return null;
            }
        }

        public static ShipProfile FromPreferences() {
            return new ShipProfile(
                Misc.Player.LocalPlayerName,
                Preferences.Instance.GetString("playerFlag"),
                Preferences.Instance.GetString("playerShipDesign"),
                Preferences.Instance.GetString("playerShipPrimaryColor"),
                Preferences.Instance.GetString("playerShipAccentColor"),
                Preferences.Instance.GetString("playerShipThrusterColor"),
                Preferences.Instance.GetString("playerShipTrailColor"),
                Preferences.Instance.GetString("playerShipHeadLightsColor")
            );
        }
    }
}