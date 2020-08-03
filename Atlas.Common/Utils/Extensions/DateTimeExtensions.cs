using System;

namespace Atlas.Common.Utils.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime TruncateToWholeMilliseconds(this DateTime dateTime) => dateTime.Truncate(TimeSpan.FromMilliseconds(1));

        private static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero)
            {
                return dateTime;
            }

            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue)
            {
                return dateTime;
            }

            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }
    }
}