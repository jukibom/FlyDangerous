using System;
using UnityEngine;

namespace Misc {
    public class TimeExtensions {
        public static string TimeSecondsToStringWithMilliseconds(float timeSeconds) {
            var t = TimeSpan.FromSeconds(Mathf.Abs(timeSeconds));
            // return in 00:00:000 format
            var time = $"{t.Minutes:D2}:{t.Seconds:D2}:{t.Milliseconds:D3}";
            return timeSeconds < 0 ? $"-{time}" : time;
        }

        public static string TimeSecondsToStringWithMillisecondTenths(float timeSeconds) {
            var t = TimeSpan.FromSeconds(Mathf.Abs(timeSeconds));
            // return in 00:00:00 format 
            var time = $"{t.Minutes:D2}:{t.Seconds:D2}:{t.Milliseconds.ToString("D2")[..2]}";
            return timeSeconds < 0 ? $"-{time}" : time;
        }
    }
}