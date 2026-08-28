using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services.HlaConversion
{
    internal interface IGGroupToPGroupConverter : IHlaConverter
    {
    }

    internal class GGroupToPGroupConverter : HlaConverterBase, IGGroupToPGroupConverter
    {
        public GGroupToPGroupConverter(
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger) : base(logger)
        {
        }

        protected override async Task<(bool WasFound, IEnumerable<string> ConvertedHla)> TryConvert(
            TargetHlaCategory? targetHlaCategory, Locus locus, string hla, IHlaMetadataDictionary hmd)
        {
            var (wasFound, pGroup) = await hmd.TryConvertGGroupToPGroup(locus, hla);

            // The single-element array is the shipped shape, and it is kept even when the P group is null: a G group of
            // null-expressing alleles is FOUND and has no P group. Staying FOUND is what matters - a not-found is
            // logged as a conversion failure and retried at the other nomenclature version - and the null then reaches
            // the null-allele rule through GenotypeConverter's SingleOrDefault().
            return (wasFound, wasFound ? new[] { pGroup } : null);
        }
    }
}