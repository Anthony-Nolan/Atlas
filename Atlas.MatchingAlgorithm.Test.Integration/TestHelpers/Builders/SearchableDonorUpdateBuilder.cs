using Atlas.DonorImport.ExternalInterface.Models;
using AutoFixture.Dsl;
using Atlas.Common.Test.SharedTestHelpers.Builders;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;

public static class SearchableDonorUpdateBuilder
{
    private const bool DefaultIsAvailableForSearch = true;
    private static readonly int DonorId = DonorIdGenerator.NextId();

    public static IPostprocessComposer<SearchableDonorUpdate> New =>
        FixtureBuilder.For<SearchableDonorUpdate>()
            .With(x => x.DonorId, DonorId)
            .With(x => x.IsAvailableForSearch, DefaultIsAvailableForSearch)
            .With(x => x.SearchableDonorInformation,
                SearchableDonorInformationBuilder.New.With(x => x.DonorId, DonorId).Build());
}