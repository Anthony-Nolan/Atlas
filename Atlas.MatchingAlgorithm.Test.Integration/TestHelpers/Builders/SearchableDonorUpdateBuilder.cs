using Atlas.DonorImport.ExternalInterface.Models;
using LochNessBuilder;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    [Builder]
    public static class SearchableDonorUpdateBuilder
    {
        private const bool DefaultIsAvailableForSearch = true;
        private static readonly int DonorId = DonorIdGenerator.NextId();

        public static Builder<SearchableDonorUpdate> New =>
            Builder<SearchableDonorUpdate>.New
                .With(x => x.DonorId, DonorId)
                .With(x => x.IsAvailableForSearch, DefaultIsAvailableForSearch)
                .With(x => x.SearchableDonorInformation, 
                    SearchableDonorInformationBuilder.New.With(x => x.DonorId, DonorId));
    }
}
