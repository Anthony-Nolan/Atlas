using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public class SerologyToPGroupsMatching
    {
        public IEnumerable<IMatchedHla> MatchSerologyToAlleles(
            IEnumerable<IMatchingPGroups> allelesToPGroups,
            IEnumerable<IMatchingSerology> serologyToSerology,
            IEnumerable<RelDnaSer> relDnaSer)
        {
            var allelesToPGroupsList = allelesToPGroups.ToList();
            var serologyToSerologyList = serologyToSerology.ToList();
            var relDnaSerList = relDnaSer.ToList();

            return serologyToSerologyList.Select(serology => GetMatchedSerology(allelesToPGroupsList, relDnaSerList, serology));
        }

        private static IMatchedHla GetMatchedSerology(
            List<IMatchingPGroups> allelesToPGroupsList,
            List<RelDnaSer> relDnaSerList,
            IMatchingSerology serologyToMatch)
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

            return new MatchedHla(
                serologyToMatch.HlaType,
                serologyToMatch.TypeUsedInMatching,
                matchingPGroups.SelectMany(m => m).Distinct(),
                serologyToMatch.MatchingSerologies);
        }
    }
}
