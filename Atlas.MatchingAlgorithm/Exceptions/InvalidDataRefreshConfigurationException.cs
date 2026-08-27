using System;

namespace Atlas.MatchingAlgorithm.Exceptions
{
    /// <summary>
    /// Thrown when data refresh settings are internally inconsistent, and so cannot be safely acted upon.
    /// </summary>
    internal class InvalidDataRefreshConfigurationException : Exception
    {
        public InvalidDataRefreshConfigurationException(string message) : base(message)
        {
        }
    }
}
