using System;

namespace Nova.Utils.Date
{
    public class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Now => DateTime.Now;
    }
}
