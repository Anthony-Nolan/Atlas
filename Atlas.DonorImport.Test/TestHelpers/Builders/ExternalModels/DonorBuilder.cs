using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels
{
    [Builder]
    public static class DonorBuilder 
    {
        private const string DefaultHlaName = "hla-name";
        private const DonorType DefaultDonorType = DonorType.Adult;

        public static Builder<Donor> New => Builder<Donor>.New
            .With(d => d.DonorId, "0")
            .With(d => d.A_1, DefaultHlaName)
            .With(d => d.A_2, DefaultHlaName)
            .With(d => d.B_1, DefaultHlaName)
            .With(d => d.B_2, DefaultHlaName)
            .With(d => d.DRB1_1, DefaultHlaName)
            .With(d => d.DRB1_2, DefaultHlaName)
            .With(d => d.DonorType, DefaultDonorType);
    }
}