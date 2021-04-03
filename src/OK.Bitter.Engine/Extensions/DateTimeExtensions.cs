using System;

namespace OK.Bitter.Engine.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToIntervalString(this TimeSpan span)
        {
            if (span.Days > 0)
            {
                return "in " + span.Days + "d";
            }
            else if (span.Hours > 0)
            {
                return "in " + span.Hours + "h";
            }
            else if (span.Minutes > 0)
            {
                return "in " + span.Minutes + "m";
            }
            else if (span.Seconds > 0)
            {
                return "in " + span.Seconds + "s";
            }
            else if (span.Milliseconds > 0)
            {
                return "in " + span.Milliseconds + "ms";
            }

            return string.Empty;
        }
    }
}
