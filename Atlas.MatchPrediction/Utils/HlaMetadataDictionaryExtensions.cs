using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Utils
{
    internal static class HlaMetadataDictionaryExtensions
    {
        /// <summary>
        /// Runs <see cref="IHlaMetadataDictionary.ConvertHla"/> for each HLA in a PhenotypeInfo, at selected loci.
        /// Excluded loci will not be converted, and will be set to null. 
        /// </summary>
        public static async Task<PhenotypeInfo<IReadOnlyCollection<string>>> ConvertAllHla(
            this IHlaMetadataDictionary hlaMetadataDictionary,
            PhenotypeInfo<string> hlaInfo,
            TargetHlaCategory targetHlaCategory,
            ISet<Locus> allowedLoci
        )
        {
            return await hlaInfo.MapAsync(async (locus, _, hla) =>
                allowedLoci.Contains(locus) ? await hlaMetadataDictionary.ConvertHla(locus, hla, targetHlaCategory) : null
            );
        }

        /// <summary>
        /// Runs <see cref="IHlaMetadataDictionary.ConvertGGroupToPGroup"/> for each HLA in a PhenotypeInfo, at selected loci.
        /// Input hla *MUST* be typed to GGroup resolution.
        /// Excluded loci will not be converted, and will be set to null. 
        /// </summary>
        public static async Task<PhenotypeInfo<string>> ConvertGGroupsToPGroups(
            this IHlaMetadataDictionary hlaMetadataDictionary,
            PhenotypeInfo<string> hlaAsGGroups,
            ISet<Locus> allowedLoci
        )
        {
            return await hlaAsGGroups.MapAsync(async (locus, _, gGroup) =>
                allowedLoci.Contains(locus) ? await hlaMetadataDictionary.ConvertGGroupToPGroup(locus, gGroup) : null
            );
        }
    }
}