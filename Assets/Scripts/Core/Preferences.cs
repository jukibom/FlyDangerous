using System;
using System.Collections.Generic;
using System.IO;
using Misc;
using Newtonsoft.Json;
using UnityEngine;

namespace Core {
    public class SaveDataVector3<T> {
        public T x;
        public T y;
        public T z;

        public override string ToString() {
            return "[ " + x + ", " + y + ", " + z + " ]";
        }
    }

    public class SaveData {
        public Dictionary<string, bool> boolPrefs;
        public Dictionary<string, float> floatPrefs;
        public Dictionary<string, string> stringPrefs;
        public Dictionary<string, SaveDataVector3<float>> vector3Prefs;

        public SaveData() {
            boolPrefs = new Dictionary<string, bool>();
            floatPrefs = new Dictionary<string, float>();
            stringPrefs = new Dictionary<string, string>();
            vector3Prefs = new Dictionary<string, SaveDataVector3<float>>();
        }

        public SaveData Clone() {
            var s = new SaveData();
            s.boolPrefs = new Dictionary<string, bool>(boolPrefs);
            s.floatPrefs = new Dictionary<string, float>(floatPrefs);
            s.stringPrefs = new Dictionary<string, string>(stringPrefs);
            s.vector3Prefs = new Dictionary<string, SaveDataVector3<float>>(vector3Prefs);
            return s;
        }

        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    public class Preferences : Singleton<Preferences> {
        private SaveData _saveData;

        public bool GetDefaultBool(string key) {
            switch (key) {
                case "showSpaceDust":
                case "autoShipRotation":
                case "graphics-terrain-details":
                    return true;

                case "graphics-ssao":
                case "graphics-soft-shadows":
                case "enableMouseFlightControls":
                case "showMouseWidget":
                case "relativeMouseXAxis":
                case "relativeMouseYAxis":
                case "mouseXInvert:":
                case "mouseYInvert:":
                case "mouseLook":
                case "forceRelativeMouseWithFAOff":
                case "enableExperimentalTerrain":
                    return false;

                default:
                    Debug.LogWarning("Attempted to get preference " + key + " with no default specified");
                    return false;
            }
        }

        public float GetDefaultFloat(string key) {
            switch (key) {
                case "graphics-terrain-geometry-lod":
                    return 70.0f;
                case "graphics-terrain-texture-distance":
                    return 5000.0f;
                case "graphics-terrain-chunks":
                    return 1.0f;
                case "graphics-field-of-view":
                    return 80.0f;
                case "graphics-field-of-view-ext":
                    return 80.0f;
                case "graphics-render-scale":
                    return 1.0f;
                case "mouseXSensitivity":
                case "mouseYSensitivity":
                    return 0.5f;
                case "mouseDeadzone":
                    return 0;
                case "mouseRelativeRate":
                    return 25.0f;
                case "mousePowerCurve":
                    return 1.0f;
                case "volumeMaster":
                case "volumeMusic":
                case "volumeSound":
                case "volumeUi":
                    return 100;
                default:
                    Debug.LogWarning("Attempted to get preference " + key + " with no default specified");
                    return 0;
            }
        }

        public string GetDefaultString(string key) {
            switch (key) {
                case "playerName":
                    return "PLAYER NAME";
                case "controlSchemeType":
                    return "arcade";
                case "flightAssistDefault":
                    return "all on";
                case "inputBindings":
                    return "";
                case "mouseXAxis":
                    return "roll";
                case "mouseYAxis":
                    return "pitch";
                case "throttleType":
                    return "full range";
                case "screen-mode":
                    return "borderless window";
                case "graphics-anti-aliasing":
                    return "none";
                case "lastUsedServerJoinAddress":
                    return "0.0.0.0";
                case "lastUsedServerJoinPort":
                    return "7777";
                case "lastUsedServerPassword":
                    return "";
                case "playerShipDesign":
                    return "Calidris";
                case "playerShipPrimaryColor":
                    return "#761012";
                case "playerShipAccentColor":
                    return "#000000";
                case "playerShipThrusterColor":
                    return "#005BBF";
                case "playerShipTrailColor":
                    return "#348A9F";
                case "playerShipHeadLightsColor":
                    return "#FFF4D6";
                case "preferredCamera":
                    return "external far";
                case "cameraMode":
                    return "absolute";
                case "mouseLookBindType":
                    return "toggle";
                default:
                    Debug.LogWarning("Attempted to get preference " + key + " with no default specified");
                    return "";
            }
        }

        public Vector3 GetDefaultVector3(string key) {
            switch (key) {
                case "hmdPosition":
                case "hmdRotation":
                    return Vector3.zero;
                default:
                    Debug.LogWarning("Attempted to get preference " + key + " with no default specified");
                    return Vector3.zero;
            }
        }


        public bool GetBool(string key) {
            return GetCurrent().boolPrefs.TryGetValue(key, out var value)
                ? value
                : GetDefaultBool(key);
        }

        public float GetFloat(string key) {
            return GetCurrent().floatPrefs.TryGetValue(key, out var value)
                ? value
                : GetDefaultFloat(key);
        }

        public string GetString(string key) {
            return GetCurrent().stringPrefs.TryGetValue(key, out var value)
                ? value
                : GetDefaultString(key);
        }

        public Vector3 GetVector3(string key) {
            return GetCurrent().vector3Prefs.TryGetValue(key, out var value)
                ? new Vector3(value.x, value.y, value.z)
                : GetDefaultVector3(key);
        }

        public void SetBool(string key, bool val) {
            GetCurrent().boolPrefs[key] = val;
        }

        public void SetFloat(string key, float val) {
            GetCurrent().floatPrefs[key] = val;
        }

        public void SetString(string key, string val) {
            GetCurrent().stringPrefs[key] = val;
        }

        public void SetVector3(string key, Vector3 val) {
            GetCurrent().vector3Prefs[key] = new SaveDataVector3<float> { x = val.x, y = val.y, z = val.z };
        }

        public SaveData GetCurrent() {
            // if already loaded, just return
            if (_saveData != null) return _saveData;

            // try to find file at save location
            try {
                var saveLoc = Path.Combine(Application.persistentDataPath, "Save", "Preferences.json");
                Debug.Log("Loading from" + saveLoc);
                using (var file = new FileStream(saveLoc, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (var reader = new StreamReader(file)) {
                        var json = reader.ReadToEnd();
                        var saveData = JsonConvert.DeserializeObject<SaveData>(json);
                        _saveData = saveData;
                    }
                }
            }
            catch (Exception e) {
                Debug.Log(e.Message);
                // no save data or failed to load - setup defaults
                Debug.Log("Loading default preferences");
                var defaults = new SaveData();
                _saveData = defaults;
            }

            return _saveData;
        }

        public void SetPreferences(SaveData saveData) {
            _saveData = saveData;
        }

        public void Save() {
            // Creates the path to the save file (make dir if needed).
            var saveLoc = Path.Combine(Application.persistentDataPath, "Save", "Preferences.json");
            var directoryLoc = Path.GetDirectoryName(saveLoc);
            if (directoryLoc != null) Directory.CreateDirectory(directoryLoc);

            var json = _saveData.ToJsonString();
            Debug.Log("Saving to " + saveLoc);

            /* A using statement is great if you plan on disposing of a stream within the same method.
               A stream should always be disposed as to free up resources that are no longer needed.
               Without using a `using` statement, you would have to manually dispose of it yourself by calling `Stream.Dispose()`.
               While `StreamWriter` can open and write to a file itself, I prefer this as it gives me more control
               over the file stream. It also allows you to reuse a stream if you want. Just make sure you tell
               the `StreamWriter` it not dispose of the underlaying stream. */
            using (var file = new FileStream(saveLoc, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                /* Another using block, this is because StreamWriter extends IDisposable,
                   Which means that it will need to be disposed of later. */
                using (var writer = new StreamWriter(file)) {
                    // StreamWriter is able to write strings out to streams.
                    writer.Write(json);
                    // Flush the data within the underlaying buffer to it's end point.
                    writer.Flush();
                }
            }
        }
    }
}