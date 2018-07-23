using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Test.Builders.ScoringInfo
{
    public class SingleAlleleScoringInfoBuilder
    {
        private SingleAlleleScoringInfo scoringInfo;

        /// <summary>
        /// Sets properties on scoring info object to default values
        /// representing an expressing allele of unknown status,
        /// with a single P/G group, and an empty list of matching serologies.
        /// </summary>
        public SingleAlleleScoringInfoBuilder()
        {
            scoringInfo = new SingleAlleleScoringInfo(
                "allele-name",
                false,
                AlleleTypingStatus.GetDefaultStatus(),
                "p-group",
                "g-group",
                new List<SerologyEntry>()
                );
        }

        public SingleAlleleScoringInfoBuilder WithAlleleName(string alleleName)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                alleleName, 
                scoringInfo.IsNullExpresser, 
                scoringInfo.AlleleTypingStatus, 
                scoringInfo.MatchingPGroup, 
                scoringInfo.MatchingGGroup, 
                scoringInfo.MatchingSerologies);

            return this;
        }

        public SingleAlleleScoringInfoBuilder WithIsNullExpresser(bool isNullExpresser)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                scoringInfo.AlleleName,
                isNullExpresser,
                scoringInfo.AlleleTypingStatus,
                scoringInfo.MatchingPGroup,
                scoringInfo.MatchingGGroup,
                scoringInfo.MatchingSerologies);

            return this;
        }

        public SingleAlleleScoringInfoBuilder WithAlleleTypingStatus(AlleleTypingStatus alleleTypingStatus)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                scoringInfo.AlleleName,
                scoringInfo.IsNullExpresser,
                alleleTypingStatus,
                scoringInfo.MatchingPGroup,
                scoringInfo.MatchingGGroup,
                scoringInfo.MatchingSerologies);

            return this;
        }
        
        public SingleAlleleScoringInfoBuilder WithMatchingPGroup(string pGroup)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                scoringInfo.AlleleName,
                scoringInfo.IsNullExpresser,
                scoringInfo.AlleleTypingStatus, 
                pGroup, 
                scoringInfo.MatchingGGroup, 
                scoringInfo.MatchingSerologies);

            return this;
        }
        
        public SingleAlleleScoringInfoBuilder WithMatchingGGroup(string gGroup)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                scoringInfo.AlleleName,
                scoringInfo.IsNullExpresser,
                scoringInfo.AlleleTypingStatus, 
                scoringInfo.MatchingPGroup, 
                gGroup, 
                scoringInfo.MatchingSerologies);

            return this;
        }

        public SingleAlleleScoringInfoBuilder WithMatchingSerologies(IEnumerable<SerologyEntry> serologyEntries)
        {
            scoringInfo = new SingleAlleleScoringInfo(
                scoringInfo.AlleleName,
                scoringInfo.IsNullExpresser,
                scoringInfo.AlleleTypingStatus, 
                scoringInfo.MatchingPGroup, 
                scoringInfo.MatchingGGroup, 
                serologyEntries);

            return this;
        }

        public SingleAlleleScoringInfo Build()
        {
            return scoringInfo;
        }
    }
}