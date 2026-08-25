using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services.HlaConversion
{
    internal interface ISmallGGroupToPGroupConverter : IHlaConverter
    {
    }

    internal class SmallGGroupToPGroupConverter : HlaConverterBase, ISmallGGroupToPGroupConverter
    {
        public SmallGGroupToPGroupConverter(
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger) : base(logger)
        {
        }

        protected override async Task<(bool WasFound, IEnumerable<string> ConvertedHla)> TryConvert(
            TargetHlaCategory? targetHlaCategory, Locus locus, string hla, IHlaMetadataDictionary hmd)
        {
            var (wasFound, pGroup) = await hmd.TryConvertSmallGGroupToPGroup(locus, hla);

            // As in GGroupToPGroupConverter: found-with-no-P-group is a real outcome for a group of null-expressing
            // alleles, and must stay FOUND rather than be logged as a failure and retried at the other version.
            return (wasFound, wasFound ? new[] { pGroup } : null);
        }
    }
}