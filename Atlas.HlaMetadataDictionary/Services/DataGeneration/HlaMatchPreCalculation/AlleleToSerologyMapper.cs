using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation
{
    internal class AlleleToSerologyMapper
    {
        public IEnumerable<MatchingSerology> GetSerologiesMatchingToAllele(IHlaInfoToMapAlleleToSerology hlaInfo, AlleleTyping alleleTyping)
        {
            var assignments = GetSerologyAssignmentsForAllele(hlaInfo.AlleleToSerologyRelationships, alleleTyping);

            var matchingSerologies = GetMatchingSerologiesFromSerologyAssignments(
                    hlaInfo.SerologyInfoForMatching,
                    alleleTyping.Locus,
                    assignments
                    );

            return matchingSerologies;
        }

        private static IEnumerable<SerologyAssignment> GetSerologyAssignmentsForAllele(
            IEnumerable<RelDnaSer> alleleToSerologyRelationships,
            IWmdaHlaTyping allele)
        {
            var relationshipForAllele = alleleToSerologyRelationships
                .SingleOrDefault(r => r.TypingEquals(allele));

            return relationshipForAllele != null
                ? relationshipForAllele.Assignments
                : new List<SerologyAssignment>();
        }

        private static IEnumerable<MatchingSerology> GetMatchingSerologiesFromSerologyAssignments(
            IEnumerable<ISerologyInfoForMatching> serologiesInfo,
            Locus locus,
            IEnumerable<SerologyAssignment> serologyAssignments)
        {
            var serologiesForLocus = serologiesInfo
                .Where(serology => serology.HlaTyping.Locus == locus);

            return serologyAssignments
                .Join(serologiesForLocus,
                    assignment => assignment.Name,
                    serology => serology.HlaTyping.Name,
                    (assignment, serology) => serology)
                .SelectMany(serology => serology.MatchingSerologies);
        }
    }
}
