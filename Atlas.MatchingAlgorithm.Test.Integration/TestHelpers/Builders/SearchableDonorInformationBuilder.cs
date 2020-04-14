using LochNessBuilder;
using Nova.DonorService.Client.Models.SearchableDonors;
using Atlas.MatchingAlgorithm.Client.Models;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    [Builder]
    public static class SearchableDonorInformationBuilder
    {
        private const string DefaultHlaName = "hla-name";
        private static readonly string DefaultDonorType = $"{DonorType.Adult}";
        private static readonly string DefaultRegistryCode = $"{RegistryCode.DKMS}";

        public static Builder<SearchableDonorInformation> New =>
            Builder<SearchableDonorInformation>.New
                .With(x => x.DonorId, DonorIdGenerator.NextId())
                .With(x => x.DonorType, DefaultDonorType)
                .With(x => x.RegistryCode, DefaultRegistryCode)
                .With(x => x.A_1, DefaultHlaName)
                .With(x => x.A_2, DefaultHlaName)
                .With(x => x.B_1, DefaultHlaName)
                .With(x => x.B_2, DefaultHlaName)
                .With(x => x.DRB1_1, DefaultHlaName)
                .With(x => x.DRB1_2, DefaultHlaName);
    }
}
