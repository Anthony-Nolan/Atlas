using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers
{
    internal static class HaplotypeFrequencyImporter
    {
        /// <returns>Id of newly imported HF set</returns>
        public static async Task<int> Import(
            IEnumerable<HaplotypeFrequency> haplotypes,
            string nomenclatureVersion,
            string registryCode = null,
            string ethnicityCode = null,
            ImportTypingCategory typingCategory = ImportTypingCategory.LargeGGroup,
            bool shouldBypassHlaValidation = false)
        {
            var registry = registryCode == null ? null : new[] { registryCode };
            var ethnicity = ethnicityCode == null ? null : new[] { ethnicityCode };

            using var file = FrequencySetFileBuilder
                .New(haplotypes, registry, ethnicity, nomenclatureVersion: nomenclatureVersion, typingCategory: typingCategory)
                .Build();

            var frequencyService = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();

            await frequencyService.ImportFrequencySet(file, shouldBypassHlaValidation 
                ? new FrequencySetImportBehaviour { ShouldBypassHlaValidation = true, ShouldConvertLargeGGroupsToPGroups = false }
                : null);

            var set = await frequencyService.GetSingleHaplotypeFrequencySet(new FrequencySetMetadata
            {
                RegistryCode = registryCode,
                EthnicityCode = ethnicityCode
            });
            
            return set.Id;
        }
    }
}