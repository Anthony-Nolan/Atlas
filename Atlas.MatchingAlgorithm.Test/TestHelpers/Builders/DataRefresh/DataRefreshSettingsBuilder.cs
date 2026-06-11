using Atlas.MatchingAlgorithm.Settings;
using AutoFixture.Dsl;
using Atlas.Common.Test.SharedTestHelpers.Builders;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;

internal static class DataRefreshSettingsBuilder
{
    public static IPostprocessComposer<DataRefreshSettings> New => FixtureBuilder.For<DataRefreshSettings>()
        .With(s => s.DormantDatabaseSize, "S0")
        .With(s => s.ActiveDatabaseSize, "S4")
        .With(s => s.RefreshDatabaseSize, "P15")
        .With(s => s.DataRefreshDonorUpdatesShouldBeFullyTransactional, false);
}