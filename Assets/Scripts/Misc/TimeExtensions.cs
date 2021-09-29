using System;

namespace Misc {
    public class TimeExtensions {
        public static string TimeSecondsToString(float timeSeconds) {
            bool addMinus = timeSeconds < 0;
            timeSeconds = Math.Abs(timeSeconds);
            
            var hours = (int) (timeSeconds / 3600);
            var minutes = (int) (timeSeconds / 60) % 60;
            var seconds = (int) timeSeconds % 60;
            var fraction = (int) (timeSeconds * 100) % 100;

            var text = hours > 0
                ? $"{hours:00}:{minutes:00}:{seconds:00}:{fraction:00}"
                : $"{minutes:00}:{seconds:00}:{fraction:00}";

            return addMinus ? $"-{text}" : text;
        }
    }
}