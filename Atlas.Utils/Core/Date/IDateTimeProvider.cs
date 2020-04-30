using System;

namespace Atlas.Utils.Core.Date
{
    /// <summary>
    /// Interface to abstract away fetching of the current date/time.
    /// Using this, we can mock what 'now' means in unit tests.
    /// </summary>
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
        DateTime Now { get; }
    }
}
