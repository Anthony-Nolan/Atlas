using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;

namespace Atlas.MatchPrediction.Utils
{
    internal static class HlaMetadataDictionaryExtensions
    {
        /// <summary>
        /// Runs <see cref="IHlaMetadataDictionary.ConvertGGroupToPGroup"/> for each HLA in a PhenotypeInfo, at selected loci.
        /// Input hla *MUST* be typed to GGroup resolution.
        /// Excluded loci will not be converted, and will be set to null.
        /// Provided nulls will be preserved.
        /// </summary>
        public static async Task<PhenotypeInfo<string>> ConvertGGroupsToPGroups(
            this IHlaMetadataDictionary hlaMetadataDictionary,
            PhenotypeInfo<string> hlaAsGGroups,
            ISet<Locus> allowedLoci
        )
        {
            return await hlaAsGGroups.MapAsync(async (locus, _, gGroup) =>
                allowedLoci.Contains(locus) && gGroup != null ? await hlaMetadataDictionary.ConvertGGroupToPGroup(locus, gGroup) : null
            );
        }

        /// <summary>
        /// Runs <see cref="IHlaMetadataDictionary.ConvertGGroupToPGroup"/> for each HLA in a LociInfo, at selected loci.
        /// Input hla *MUST* be typed to GGroup resolution.
        /// Excluded loci will not be converted, and will be set to null. 
        /// Provided nulls will be preserved.
        /// </summary>
        public static async Task<LociInfo<string>> ConvertGGroupsToPGroups(
            this IHlaMetadataDictionary hlaMetadataDictionary,
            LociInfo<string> hlaAsGGroups,
            ISet<Locus> allowedLoci
        )
        {
            return await hlaAsGGroups.MapAsync(async (locus, gGroup) =>
                allowedLoci.Contains(locus) && gGroup != null ? await hlaMetadataDictionary.ConvertGGroupToPGroup(locus, gGroup) : null
            );
        }
    }
}