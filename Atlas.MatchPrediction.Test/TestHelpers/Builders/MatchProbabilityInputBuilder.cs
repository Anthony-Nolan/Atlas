using Atlas.Common.GeneticData.PhenotypeInfo;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.MatchProbabilityInput>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    internal static class MatchProbabilityInputBuilder
    {
        internal static Builder New => Builder.New
            .With(i => i.PatientHla, new PhenotypeInfo<string>("hla"))
            .With(i => i.DonorHla, new PhenotypeInfo<string>("hla"))
            .With(i => i.HlaNomenclatureVersion, "nomenclature-version");
    }
}