using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Misc {
    public static class EnumExtensions {
        
        // Use reflection to determine a [Description("Some Human Readable String")] from an enum value declaration 
        public static string DescriptionAtt<T>(T value) where T : Enum
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }
    }
}