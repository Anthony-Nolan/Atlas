using Atlas.MatchingAlgorithm.Settings;
using LochNessBuilder;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh
{
    [Builder]
    public static class DataRefreshSettingsBuilder
    {
        public static Builder<DataRefreshSettings> New
        {
            get
            {
                return Builder<DataRefreshSettings>.New
                    .With(s => s.DormantDatabaseSize, "S0")
                    .With(s => s.ActiveDatabaseSize, "S4")
                    .With(s => s.RefreshDatabaseSize, "P15");
            }
        }
    }
}