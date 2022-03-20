using UnityEngine;

namespace Misc {
    public class SimpleDebug {
        public static void Log(params object[] args) {
            var formatString = "";
            for (var i = 0; i < args.Length; i++) {
                if (i > 0) formatString += ", ";
                formatString += "{" + i + "}";
            }

            Debug.LogFormat(formatString, args);
        }
    }
}