using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.SingleDonorMatchProbabilityInput>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs
{
    [Builder]
    public static class SingleDonorMatchProbabilityInputBuilder
    {
        public static Builder Default => Builder.New
            .With(i => i.Donor, new DonorInput { DonorId = 123 })
            .WithPatientHla(new PhenotypeInfo<string>("hla"))
            .WithDonorHla(new PhenotypeInfo<string>("hla"));

        /// <summary>
        /// Has all the required fields set
        /// </summary>
        public static Builder Valid => Builder.New
            .WithExcludedLoci(Locus.Dpb1)
            .WithPatientHla(new PhenotypeInfo<string>("hla"))
            .With(i => i.Donor, new DonorInput { DonorId = 123 })
            .WithDonorHla(new PhenotypeInfo<string>("hla"));

        public static Builder WithDonorId(this Builder builder, int id)
        {
            var donorInput = builder.Build().Donor;
            donorInput.DonorId = id;
            return builder.With(i => i.Donor, donorInput);
        }

        public static Builder WithDonorHla(this Builder builder, PhenotypeInfo<string> donorHla)
        {
            var donorInput = builder.Build().Donor;
            donorInput.DonorHla = donorHla?.ToPhenotypeInfoTransfer();
            return builder.With(i => i.Donor, donorInput);
        }

        public static Builder WithDonorMetadata(this Builder builder, FrequencySetMetadata frequencySetMetadata)
        {
            var donorInput = builder.Build().Donor;
            donorInput.DonorFrequencySetMetadata = frequencySetMetadata;
            return builder.With(i => i.Donor, donorInput);
        }

        public static Builder WithPatientHla(this Builder builder, PhenotypeInfo<string> patientHla) =>
            builder.With(i => i.PatientHla, patientHla?.ToPhenotypeInfoTransfer());

        public static Builder WithPatientMetadata(this Builder builder, FrequencySetMetadata frequencySetMetadata) =>
            builder.With(i => i.PatientFrequencySetMetadata, frequencySetMetadata);

        public static Builder WithExcludedLoci(this Builder builder, params Locus[] loci) => builder.With(i => i.ExcludedLoci, loci);
    }
}