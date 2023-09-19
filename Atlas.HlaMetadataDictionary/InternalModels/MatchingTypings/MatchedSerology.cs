using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal class MatchedSerology : IMatchedHla, IHlaMetadataSource<SerologyTyping>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public List<string> MatchingPGroups { get; }
        public List<string> MatchingGGroups { get; }
        public IEnumerable<MatchingSerology> MatchingSerologies { get; }
        public SerologyTyping TypingForHlaMetadata => (SerologyTyping) HlaTyping;

        /// <summary>
        /// Info that can be used to determine why this <see cref="MatchedSerology"/> has been assigned <see cref="MatchingPGroups"/> and <see cref="MatchingGGroups"/>.
        /// </summary>
        public IEnumerable<SerologyToAlleleMapping> SerologyToAlleleMappings { get; set; }

        public MatchedSerology(
            SerologyInfoForMatching matchedSerology,
            List<string> matchingPGroups, 
            List<string> matchingGGroups,
            IEnumerable<SerologyToAlleleMapping> serologyToAlleleMappings
            )
        {
            HlaTyping = matchedSerology.HlaTyping;
            TypingUsedInMatching = matchedSerology.TypingUsedInMatching;
            MatchingPGroups = matchingPGroups;
            MatchingGGroups = matchingGGroups;
            MatchingSerologies = matchedSerology.MatchingSerologies;
            SerologyToAlleleMappings = serologyToAlleleMappings;
        }     
    }
}