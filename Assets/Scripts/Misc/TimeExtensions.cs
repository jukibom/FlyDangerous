using System;
using UnityEngine;

namespace Misc {
    public class TimeExtensions {
        public static string TimeSecondsToString(float timeSeconds) {
            var t = TimeSpan.FromSeconds(Mathf.Abs(timeSeconds));
            // return in 00:00:00 format (tens of milliseconds - we're only accurate to 1/50th of a second anyway!)
            var time = $"{t.Minutes:D2}:{t.Seconds:D2}:{t.Milliseconds:D3}";
            return timeSeconds < 0 ? $"-{time}" : time;
        }
    }
}