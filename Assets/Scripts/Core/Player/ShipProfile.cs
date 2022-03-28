using System;
using Core.ShipModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Player {
    public class ShipProfile {
        public readonly string playerName;
        public readonly string shipModel; 
        public readonly string primaryColor; 
        public readonly string accentColor;
        public readonly string thrusterColor; 
        public readonly string trailColor; 
        public readonly string headLightsColor;

        public ShipProfile(string playerName, string shipModel, string primaryColor, string accentColor, string thrusterColor, string trailColor, string headLightsColor) {
            this.playerName = playerName;
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
                Preferences.Instance.GetString("playerName"),
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