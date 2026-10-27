using System;
using System.Text;
using UnityEngine;

namespace Misc {
    public class TimeExtensions {
        // Shared to avoid allocating a new StringBuilder per call.
        // Not thread-safe!
        private static readonly StringBuilder TimeStringBuilder = new(12);

        public static string TimeSecondsToStringWithMilliseconds(float timeSeconds) {
            var t = TimeSpan.FromSeconds(Mathf.Abs(timeSeconds));

            TimeStringBuilder.Clear();

            if (timeSeconds < 0) {
                TimeStringBuilder.Append('-');
            }

            AppendTwoDigits(t.Minutes);
            TimeStringBuilder.Append(':');
            AppendTwoDigits(t.Seconds);
            TimeStringBuilder.Append(':');
            AppendThreeDigits(t.Milliseconds);

            return TimeStringBuilder.ToString();
        }

        public static string TimeSecondsToStringWithMillisecondTenths(float timeSeconds) {
            var t = TimeSpan.FromSeconds(Mathf.Abs(timeSeconds));

            TimeStringBuilder.Clear();

            if (timeSeconds < 0) {
                TimeStringBuilder.Append('-');
            }

            AppendTwoDigits(t.Minutes);
            TimeStringBuilder.Append(':');
            AppendTwoDigits(t.Seconds);
            TimeStringBuilder.Append(':');
            AppendTwoDigits(t.Milliseconds / 10);

            return TimeStringBuilder.ToString();
        }

        private static void AppendTwoDigits(int value) {
            TimeStringBuilder.Append((char)('0' + value / 10));
            TimeStringBuilder.Append((char)('0' + value % 10));
        }

        private static void AppendThreeDigits(int value) {
            TimeStringBuilder.Append((char)('0' + value / 100));
            TimeStringBuilder.Append((char)('0' + value / 10 % 10));
            TimeStringBuilder.Append((char)('0' + value % 10));
        }
    }
}