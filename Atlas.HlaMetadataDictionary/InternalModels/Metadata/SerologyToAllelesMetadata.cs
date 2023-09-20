using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;

namespace Atlas.HlaMetadataDictionary.InternalModels.Metadata
{
    internal interface ISerologyToAllelesMetadata : ISerialisableHlaMetadata
    {
        List<SerologyToAlleleMappingSummary> SerologyToAlleleMappings { get; }
    }

    internal class SerologyToAllelesMetadata : SerialisableHlaMetadata, ISerologyToAllelesMetadata
    {
        public List<SerologyToAlleleMappingSummary> SerologyToAlleleMappings { get; }
        public override object HlaInfoToSerialise => SerologyToAlleleMappings;

        /// <summary>
        /// Used during HMD data generation
        /// </summary>
        public SerologyToAllelesMetadata(MatchedSerology serology)
            : base(serology.HlaTyping.Locus, serology.HlaTyping.Name, TypingMethod.Serology)
        {
            SerologyToAlleleMappings = SummariseSerologyToAlleleMappings(serology.SerologyToAlleleMappings);
        }

        /// <summary>
        /// Used when retrieving metadata from the HMD
        /// </summary>
        public SerologyToAllelesMetadata(Locus locus, string serologyName, List<SerologyToAlleleMappingSummary> mappings) 
            : base(locus, serologyName, TypingMethod.Serology)
        {
            SerologyToAlleleMappings = mappings;
        }

        private static List<SerologyToAlleleMappingSummary> SummariseSerologyToAlleleMappings(IEnumerable<SerologyToAlleleMapping> mappings)
        {
            return mappings
                .GroupBy(m => m.MatchedAllele.MatchingPGroup)
                .Select(grp => new SerologyToAlleleMappingSummary
                {
                    PGroup = grp.Key,
                    GGroups = grp.Select(g => g.MatchedAllele.MatchingGGroup).Distinct().OrderBy(x => x).ToList(),
                    Alleles = grp.Select(g => g.MatchedAllele.HlaTyping.Name).Distinct().OrderBy(x => x).ToList(),
                    SerologyBridge = grp.SelectMany(g => g.SerologyBridge).Distinct().OrderBy(x => x).ToList()
                }).ToList();
        }
    }
}