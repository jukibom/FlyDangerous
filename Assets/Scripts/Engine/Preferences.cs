using System.IO;
// using Newtonsoft.Json;
using UnityEngine;

namespace Engine {
    public struct SaveData {
        public string inputBindings;
        public bool flightAssistOnByDefault;
    }

    public class Preferences {

        private static SaveData _saveData;
        private bool _loaded = false;

            public SaveData GetCurrent() {
            // if already loaded, just return
            if (_loaded) {
                return _saveData; 
            }

            // setup defaults
            SaveData defaults;
            defaults.inputBindings = "";
            defaults.flightAssistOnByDefault = true;

            // try to find file at save location
            try {
                var saveLoc = Path.Combine(Application.persistentDataPath, "Save", "Preferences.json");
                using (var file = new FileStream(saveLoc, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (var reader = new StreamReader(file)) {
                        var json = reader.ReadToEnd();
                        // return JsonConvert.DeserializeObject<SaveData>(json);
                    }
                }
            }
            catch {
                return defaults;
            }

            return defaults;
            }

        public static void Save(SaveData data) {
            _saveData = data;
            
            // Creates the path to the save file.
            var saveLoc = Path.Combine(Application.persistentDataPath, "Save", "Preferences.json");
            // Tells Newtonsoft to convert this object to a JSON.
            // var json = JsonConvert.SerializeObject(data);

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
                    // writer.Write(json);
                    // Flush the data within the underlaying buffer to it's end point.
                    writer.Flush();
                }
            }
        }
    }
}