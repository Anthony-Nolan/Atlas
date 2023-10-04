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

        protected override async Task<IEnumerable<string>> ConvertHla(
            TargetHlaCategory? targetHlaCategory, Locus locus, string hla, IHlaMetadataDictionary hmd)
        {
            return new[] { await hmd.ConvertSmallGGroupToPGroup(locus, hla) };
        }
    }
}