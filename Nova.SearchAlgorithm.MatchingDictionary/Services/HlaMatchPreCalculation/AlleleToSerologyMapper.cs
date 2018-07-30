using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation
{
    internal class AlleleToSerologyMapper
    {
        public IEnumerable<SerologyMappingForAllele> GetSerologyMappingsForAllele(IHlaInfoToMapAlleleToSerology hlaInfo, AlleleTyping alleleTyping)
        {
            var alleleFamilyAsTyping = ConvertAlleleFamilyToHlaTypingWithSerologyLocus(alleleTyping);

            var assignments = GetSerologyAssignmentsForAlleleIfExists(hlaInfo.AlleleToSerologyRelationships, alleleTyping);

            var mappingInfo = GetMappingInfoFromSerologyAssignments(
                    hlaInfo.SerologyInfoForMatching,
                    assignments,
                    alleleFamilyAsTyping);

            return mappingInfo;
        }

        private static HlaTyping ConvertAlleleFamilyToHlaTypingWithSerologyLocus(IWmdaHlaTyping allele)
        {
            var serologyLocus = PermittedLocusNames.GetSerologyLocusNameFromMolecularIfExists(allele.Locus);
            var serologyName = allele.Name.Split(':')[0].TrimStart('0');
            return new HlaTyping(TypingMethod.Serology, serologyLocus, serologyName);
        }

        private static IEnumerable<SerologyAssignment> GetSerologyAssignmentsForAlleleIfExists(
            IEnumerable<RelDnaSer> alleleToSerologyRelationships,
            IWmdaHlaTyping allele)
        {
            var relationshipForAllele = alleleToSerologyRelationships.SingleOrDefault(r =>
                r.Locus.Equals(allele.Locus) && r.Name.Equals(allele.Name));

            return relationshipForAllele != null ?
                relationshipForAllele.Assignments : new List<SerologyAssignment>();
        }

        private static IEnumerable<SerologyMappingForAllele> GetMappingInfoFromSerologyAssignments(
            IList<ISerologyInfoForMatching> serologiesInfo,
            IEnumerable<SerologyAssignment> assignmentsForAllele,
            HlaTyping alleleFamilyAsTyping)
        {
            var expectedMatchingSerology = serologiesInfo
                    .FirstOrDefault(m => m.HlaTyping.Equals(alleleFamilyAsTyping))
                    ?.MatchingSerologies;

            var mappingInfoQuery =
                from serology in serologiesInfo
                join assigned in assignmentsForAllele
                    on serology.TypingUsedInMatching.Name equals assigned.Name
                where serology.TypingUsedInMatching.MatchLocus.Equals(alleleFamilyAsTyping.MatchLocus)
                select new SerologyMappingForAllele(
                    (SerologyTyping)serology.HlaTyping,
                    assigned.Assignment,
                    serology.MatchingSerologies.Select(actualMatchingSerology =>
                        GetSerologyMatchInfo(
                            actualMatchingSerology.SerologyTyping, 
                            alleleFamilyAsTyping, 
                            expectedMatchingSerology)));

            return mappingInfoQuery;
        }

        private static SerologyMatchToAllele GetSerologyMatchInfo(
            SerologyTyping actualMatchingSerology,
            HlaTyping alleleFamilyAsTyping,
            IEnumerable<MatchingSerology> expectedMatchingSerologies
        )
        {
            var matchInfo = new SerologyMatchToAllele(actualMatchingSerology);

            if (actualMatchingSerology.IsDeleted
                || UnexpectedAlleleToSerologyMappings.PermittedExceptions.Contains(alleleFamilyAsTyping))
            {
                return matchInfo;
            }

            matchInfo.IsUnexpected =
                expectedMatchingSerologies == null ||
                !expectedMatchingSerologies
                .Select(ser => ser.SerologyTyping)
                .Contains(actualMatchingSerology);

            return matchInfo;
        }
    }
}
