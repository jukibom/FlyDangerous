#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Core.MapData {
    public class Location {
        // Declare locations here and add to the List() function below
        public static Location Space => new Location(0, "Space", "Space", false);
        public static Location TestSpaceStation => new Location(1, "Test Space Station", "SpaceStation", false);
        public static Location TerrainV1 => new Location(2, "Flat World", "TerrainV1", true);
        public static Location TerrainV2 => new Location(3, "Canyons", "TerrainV2", true);
        public static Location TerrainV3 => new Location(4, "Biome World", "TerrainV3", true);
    
        
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string SceneToLoad { get; private set; }
        public bool IsTerrain { get; private set; }

        private Location(int id, string name, string sceneToLoad, bool isTerrain) {
            Id = id;
            Name = name;
            SceneToLoad = sceneToLoad; 
            IsTerrain = isTerrain;
        }

        public static IEnumerable<Location> List() {
            return new[] { Space, TestSpaceStation, TerrainV1, TerrainV2, TerrainV3 };
        }

        public static Location FromString(string locationString) {
            return List().Single(l => l.Name == locationString);
        }

        public static Location FromId(int id) {
            return List().Single(l => l.Id == id);
        }
        
        // TODO: this should really be elsewhere and generic based on a sane enum helper type
        public static void PopulateDropDown(
            Dropdown dropdown, 
            Func<string, string>? textTransform = null
        ) {
            var values = List();
            var newOptions = new List<Dropdown.OptionData>();
            
            foreach (var location in values) {
                var option = textTransform != null ? textTransform(location.Name) : location.Name;
                newOptions.Add(new Dropdown.OptionData(option));
            }
 
            dropdown.ClearOptions();
            dropdown.AddOptions(newOptions);
        }

        public override string ToString() {
            return Name;
        }
    }

    public class LocationJsonConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            if (value is Location location) {
                writer.WriteValue(location.Name);
            }
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.String) {
                throw new JsonSerializationException();
            }

            var location = serializer.Deserialize<string>(reader);
            return location != null ? Location.FromString(location) : null;
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(string);
        }
    }
}