using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Engine {
    public class SaveData {
        public Dictionary<string, bool> boolPrefs;
        public Dictionary<string, float> floatPrefs;
        public Dictionary<string, string> stringPrefs;
        
        public SaveData() {
            boolPrefs = new Dictionary<string, bool>();
            floatPrefs = new Dictionary<string, float>();
            stringPrefs = new Dictionary<string, string>();
        }

        public SaveData Clone() {
            SaveData s = new SaveData();
            s.boolPrefs = new Dictionary<string, bool>(boolPrefs);
            s.floatPrefs = new Dictionary<string, float>(floatPrefs);
            s.stringPrefs = new Dictionary<string, string>(stringPrefs);
            return s;
        }

        public string ToJsonString() {
            // Tells Newtonsoft to convert this object to a JSON.
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    public class Preferences : MonoBehaviour {

        public static Preferences Instance;
        private SaveData _saveData;

        void Awake() {
            // singleton shenanigans
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        public bool GetDefaultBool(string key) {
            switch (key) {
                case "showSpaceDust":
                case "enableMouseFlightControls":
                case "showMouseWidget":
                    return true;
                
                case "relativeMouseXAxis":
                case "relativeMouseYAxis":
                case "flightAssistOnByDefault": 
                case "enableTerrainScaling":
                    return false;
                
                default: 
                    Debug.LogWarning("Attempted to get preference " + key + " with no default specified");
                    return false;
            }
        }
        
        public float GetDefaultFloat(string key) {
            switch (key) {
                case "mouseXSensitivity":
                case "mouseYSensitivity":
                    return 1f;
                default: 
                    Debug.LogWarning("Attempted to get preference " + key + " with no default specified");
                    return 0;
            }
        }
         
        public string GetDefaultString(string key) {
            switch (key) {
                case "inputBindings":
                    return "";
                case "mouseXAxis":
                    return "roll";
                case "mouseYAxis":
                    return "pitch";
                default: 
                    Debug.LogWarning("Attempted to get preference " + key + " with no default specified");
                    return "";
            }
        }

        
        public bool GetBool(string key) {
            bool value;
            return GetCurrent().boolPrefs.TryGetValue(key, out value)
                ? value
                : GetDefaultBool(key);
        }
        
        public float GetFloat(string key) {
            float value;
            return GetCurrent().floatPrefs.TryGetValue(key, out value)
                ? value
                : GetDefaultFloat(key);
        }
        
        public string GetString(string key) {
            string value;
            return GetCurrent().stringPrefs.TryGetValue(key, out value)
                ? value
                : GetDefaultString(key);
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

        public SaveData GetCurrent() {
            // if already loaded, just return
            if (_saveData != null) {
                return _saveData; 
            }

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
                SaveData defaults = new SaveData();
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
            if (directoryLoc != null) {
                Directory.CreateDirectory(directoryLoc);
            }

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