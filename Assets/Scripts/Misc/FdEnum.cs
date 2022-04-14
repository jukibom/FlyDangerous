#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Misc {
    public interface IFdEnum {
        public int Id { get; }
        public string Name { get; }
    }

    public static class FdEnum {
        public static T FromId<T>(IEnumerable<T> enums, int id) where T : IFdEnum {
            var fdEnums = enums as T[] ?? enums.ToArray();
            try {
                return fdEnums.Single(l => l.Id == id);
            }
            catch {
                var firstElement = fdEnums.First();
                Debug.Log($"Failed to find enum with id {id}, returning first element {firstElement.Name}");
                return firstElement;
            }
        }

        public static T FromString<T>(IEnumerable<T> enums, string name) where T : IFdEnum {
            var fdEnums = enums as T[] ?? enums.ToArray();
            try {
                return fdEnums.Single(l => l.Name == name);
            }
            catch {
                var firstElement = fdEnums.First();
                Debug.Log($"Failed to parse enum string {name}, returning first element {firstElement.Name}");
                return firstElement;
            }
        }

        public static void PopulateDropDown<T>(
            IEnumerable<T> enums,
            Dropdown dropdown,
            Func<string, string>? textTransform = null,
            Func<T, Sprite>? useIcon = null
        ) where T : IFdEnum {
            var newOptions = new List<Dropdown.OptionData>();

            foreach (var option in enums) {
                var dropDownOption = textTransform != null ? textTransform(option.Name) : option.Name;
                var data = new Dropdown.OptionData(dropDownOption);

                if (useIcon != null) {
                    var icon = useIcon(option);
                    data.image = icon;
                }

                newOptions.Add(data);
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(newOptions);
        }

        public static T FromDropdownId<T>(IEnumerable<T> enums, int id) {
            var enumerable = enums as T[] ?? enums.ToArray();
            if (enumerable.Count() >= id + 1) return enumerable.ToArray()[id];

            return enumerable.First();
        }

        public static int ToDropdownId(IEnumerable<IFdEnum> enums, IFdEnum element) {
            var targetId = element.Id;
            var fdEnums = enums as IFdEnum[] ?? enums.ToArray();
            var flag = fdEnums.First(f => f.Id == targetId);
            var dropdownId = Array.IndexOf(fdEnums, flag);
            return dropdownId != -1 ? dropdownId : 0;
        }
    }

    public class FdEnumJsonConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            if (value is IFdEnum fdEnum) writer.WriteValue(fdEnum.Name);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.String) throw new JsonSerializationException();

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