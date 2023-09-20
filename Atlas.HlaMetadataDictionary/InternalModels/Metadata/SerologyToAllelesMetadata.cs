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
            SerologyToAlleleMappings = SummariseSerologyToAlleleMapping(serology.SerologyToAlleleMappings).ToList();
        }

        /// <summary>
        /// Used when retrieving metadata from the HMD
        /// </summary>
        public SerologyToAllelesMetadata(Locus locus, string serologyName, List<SerologyToAlleleMappingSummary> mappings)
            : base(locus, serologyName, TypingMethod.Serology)
        {
            SerologyToAlleleMappings = mappings;
        }

        private static IEnumerable<SerologyToAlleleMappingSummary> SummariseSerologyToAlleleMapping(IEnumerable<SerologyToAlleleMapping> mappings)
        {
            return mappings
                .SelectMany(m => m.SerologyBridge
                    .Select(s => new
                    {
                        SerologyBridge = s,
                        PGroup = m.MatchedAllele.MatchingPGroup,
                        GGroup = m.MatchedAllele.MatchingGGroup,
                        Allele = m.MatchedAllele.HlaTyping.Name
                    }))
                .GroupBy(m => new { m.SerologyBridge, m.PGroup })
                .Select(grp => new SerologyToAlleleMappingSummary
                {
                    SerologyBridge = grp.Key.SerologyBridge,
                    PGroup = grp.Key.PGroup,
                    GGroups = grp.Select(g => g.GGroup).Distinct().ToList(),
                    Alleles = grp.Select(g => g.Allele).Distinct().ToList()
                });
        }
    }
}