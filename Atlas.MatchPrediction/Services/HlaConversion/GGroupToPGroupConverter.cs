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

        protected override async Task<IEnumerable<string>> ConvertHla(
            TargetHlaCategory? targetHlaCategory, Locus locus, string hla, IHlaMetadataDictionary hmd)
        {
            return new[] { await hmd.ConvertGGroupToPGroup(locus, hla) };
        }
    }
}