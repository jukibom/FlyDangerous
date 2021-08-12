using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.UI;

namespace Misc {
    public static class EnumExtensions {
        
        // Use reflection to determine a [Description("Some Human Readable String")] from an enum value declaration 
        public static string DescriptionAtt(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            // if we have a description attribute, return it (or the first if many)
            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            // else return the basic name
            return Enum.GetName(value.GetType(), value);
        }
        
        // Given a unity dropdown element, clear and populate with descriptive values corresponding to the enum index.
        // May optionally provide a transform to mutate the description string.
        public static void PopulateDropDownWithEnum<T>(
            Dropdown dropdown, 
            [CanBeNull] Func<string, string> textTransform = null
        ) where T : Enum {
            var values = (T[])Enum.GetValues(typeof(T));
            var newOptions = new List<Dropdown.OptionData>();
            
            foreach (var t in values) {
                var description = DescriptionAtt(t);
                var option = textTransform != null ? textTransform(description) : description;
                newOptions.Add(new Dropdown.OptionData(option));
            }
 
            dropdown.ClearOptions();
            dropdown.AddOptions(newOptions);
        }
    }
}