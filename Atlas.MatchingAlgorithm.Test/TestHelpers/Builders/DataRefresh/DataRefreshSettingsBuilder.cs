using Atlas.MatchingAlgorithm.Settings;
using LochNessBuilder;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh
{
    [Builder]
    internal static class DataRefreshSettingsBuilder
    {
        public static Builder<DataRefreshSettings> New => Builder<DataRefreshSettings>.New
            .With(s => s.DormantDatabaseSize, "S0")
            .With(s => s.ActiveDatabaseSize, "S4")
            .With(s => s.RefreshDatabaseSize, "P15")
            .With(s => s.DataRefreshDonorUpdatesShouldBeFullyTransactional, false);
    }
}