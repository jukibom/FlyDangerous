using UnityEngine;

namespace Misc {
    public static class ColorExtensions {
        public static Color ParseHtmlColor(string htmlColor) {
            if (ColorUtility.TryParseHtmlString(htmlColor, out var color)) return color;
            color = Color.red;
            Debug.LogWarning("Failed to parse html color " + htmlColor);

            return color;
        }
    }
}