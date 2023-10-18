using System;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services.HlaConversion
{
    internal interface IHlaToTargetCategoryConverter : IHlaConverter
    {
    }

    internal class HlaToTargetCategoryConverter : HlaConverterBase, IHlaToTargetCategoryConverter
    {
        public HlaToTargetCategoryConverter(
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            IMatchPredictionLogger<MatchProbabilityLoggingContext> logger) : base(logger)
        {
        }

        protected override async Task<IEnumerable<string>> ConvertHla(
            TargetHlaCategory? targetHlaCategory, Locus locus, string hla, IHlaMetadataDictionary hmd)
        {
            if (targetHlaCategory == null)
            {
                throw new ArgumentNullException(nameof(targetHlaCategory));
            }

            return await hmd.ConvertHla(locus, hla, targetHlaCategory.Value);
        }
    }
}