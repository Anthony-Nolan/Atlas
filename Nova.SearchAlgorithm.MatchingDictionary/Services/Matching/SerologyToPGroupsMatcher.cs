using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    public class SerologyToPGroupsMatcher
    {
        public IEnumerable<MatchedSerology> MatchSerologyToPGroups(MatchedHlaSourceData sourceData)
        {
            return sourceData.SerologyToSerology.Select(serology =>
                GetMatchedSerology(sourceData.AlleleToPGroups, sourceData.RelDnaSer, serology));
        }

        private static MatchedSerology GetMatchedSerology(
            List<IAlleleToPGroup> allelesToPGroupsList,
            List<RelDnaSer> relDnaSerList,
            ISerologyToSerology serologyToMatch)
        {
            var matchLocus = serologyToMatch.TypeUsedInMatching.MatchLocus;
            var matchingSerology = serologyToMatch.MatchingSerologies.Select(m => m.Name);

            var matchingPGroups =
                from allele in allelesToPGroupsList
                join dnaToSer in relDnaSerList
                    on new { allele.TypeUsedInMatching.WmdaLocus, allele.TypeUsedInMatching.Name }
                    equals new { dnaToSer.WmdaLocus, dnaToSer.Name }
                where allele.TypeUsedInMatching.MatchLocus.Equals(matchLocus)
                      && dnaToSer.Serologies.Intersect(matchingSerology).Any()
                select allele.MatchingPGroups;

            return new MatchedSerology(
                serologyToMatch,
                matchingPGroups.SelectMany(m => m).Distinct());
        }
    }
}
