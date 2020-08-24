using System;
using Polly;
using Polly.Retry;

namespace Atlas.MatchPrediction.Data
{
    internal static class RetryConfig
    {
        private static readonly TimeSpan[] RetrySleepDurations = {TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)};

        internal static readonly AsyncRetryPolicy AsyncRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(RetrySleepDurations);
    }
}