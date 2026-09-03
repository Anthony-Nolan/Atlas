using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;

// Four aliases rather than four PhenotypeInfo<string>/LociInfo<string>, because the direction of this conversion is
// the entire content of both methods and the type says nothing about it - the constraint would otherwise live only in
// prose ("Input hla *MUST* be typed to GGroup resolution", twice, below). Aliases, not wrapper types: an alias erases
// to string, so this is documentation at the declaration, not type safety.
using GGroupGenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using PGroupGenotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;
using GGroupHaplotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;
using PGroupHaplotype = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

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
        public static async Task<PGroupGenotype> ConvertGGroupsToPGroups(
            this IHlaMetadataDictionary hlaMetadataDictionary,
            GGroupGenotype hlaAsGGroups,
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
        public static async Task<PGroupHaplotype> ConvertGGroupsToPGroups(
            this IHlaMetadataDictionary hlaMetadataDictionary,
            GGroupHaplotype hlaAsGGroups,
            ISet<Locus> allowedLoci
        )
        {
            return await hlaAsGGroups.MapAsync(async (locus, gGroup) =>
                allowedLoci.Contains(locus) && gGroup != null ? await hlaMetadataDictionary.ConvertGGroupToPGroup(locus, gGroup) : null
            );
        }
    }
}