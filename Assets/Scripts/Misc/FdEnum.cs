#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.UI;

namespace Misc {
    public interface IFdEnum {
        public int Id { get; }
        public string Name { get; }
    }

    public static class FdEnum {
        
        public static T FromId<T>(IEnumerable<T> enums, int id) where T : IFdEnum {
            return enums.Single(l => l.Id == id);
        }
        
        public static T FromString<T>(IEnumerable<T> enums, string name) where T : IFdEnum {
            return enums.Single(l => l.Name == name);
        }
        
        public static void PopulateDropDown<T>(
            IEnumerable<T> enums,
            Dropdown dropdown, 
            Func<string, string>? textTransform = null
        ) where T : IFdEnum {
            var newOptions = new List<Dropdown.OptionData>();
            
            foreach (var location in enums) {
                var option = textTransform != null ? textTransform(location.Name) : location.Name;
                newOptions.Add(new Dropdown.OptionData(option));
            }
 
            dropdown.ClearOptions();
            dropdown.AddOptions(newOptions);
        }
    }
    
    public class FdEnumJsonConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            if (value is IFdEnum fdEnum) {
                writer.WriteValue(fdEnum.Name);
            }
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.String) {
                throw new JsonSerializationException();
            }
            
            // find the static FromString method on the enum derived type with DISGUSTING REFLECTION.
            // I'm at peace with this.
            var name = serializer.Deserialize<string>(reader);
            var method = objectType.GetMethod("FromString");
            return name != null && method != null ? method.Invoke(objectType, new object[] { name }) : null;
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(string);
        }
    }
}