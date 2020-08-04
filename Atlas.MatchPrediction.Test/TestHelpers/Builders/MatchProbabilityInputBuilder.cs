using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.MatchProbabilityInput>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    public static class MatchProbabilityInputBuilder
    {
        public static Builder New => Builder.New
            .WithPatientHla(new PhenotypeInfo<string>("hla"))
            .WithDonorHla(new PhenotypeInfo<string>("hla"))
            .With(i => i.HlaNomenclatureVersion, "nomenclature-version");

        public static Builder WithDonorHla(this Builder builder, PhenotypeInfo<string> donorHla) =>
            builder.With(i => i.DonorHla, donorHla.ToPhenotypeInfoTransfer());

        public static Builder WithPatientHla(this Builder builder, PhenotypeInfo<string> patientHla) =>
            builder.With(i => i.PatientHla, patientHla.ToPhenotypeInfoTransfer());
    }
}