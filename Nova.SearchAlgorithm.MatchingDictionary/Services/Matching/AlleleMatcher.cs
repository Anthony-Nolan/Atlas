using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Matching
{
    /// <summary>
    /// Creates a complete collection of matched alleles
    /// from the information that was extracted from the WMDA files.
    /// </summary>
    internal class AlleleMatcher : IHlaMatcher
    {
        public IEnumerable<IMatchedHla> CreateMatchedHla(HlaInfoForMatching hlaInfo)
        {
            return hlaInfo.AlleleInfoForMatching.Select(alleleInfo =>
                    new MatchedAllele(alleleInfo, GetSerologyMappingsForAllele(hlaInfo, alleleInfo.TypingUsedInMatching))
                    );
        }

        private static IList<DnaToSerologyMapping> GetSerologyMappingsForAllele(IHlaInfoToMapSerologyToAllele hlaInfo, HlaTyping allele)
        {
            var alleleFamilyAsTyping = ConvertAlleleFamilyToHlaTypingWithSerologyLocus(allele);

            var assignments = GetSerologyAssignmentsForAlleleIfExists(hlaInfo.DnaToSerologyRelationships, allele);

            var mappingInfo = GetMappingInfoFromDnaToSerologyRelationships(
                    hlaInfo.SerologyInfoForMatching,
                    assignments,
                    alleleFamilyAsTyping);

            if (IsAlleleFamilyInvalidSerology(hlaInfo.SerologyInfoForMatching, allele, alleleFamilyAsTyping))
                mappingInfo.Add(CreateMappingFromAlleleFamily(alleleFamilyAsTyping));

            return mappingInfo;
        }

        private static HlaTyping ConvertAlleleFamilyToHlaTypingWithSerologyLocus(IWmdaHlaTyping allele)
        {
            var serologyLocus = LocusNames.GetSerologyLocusNameFromMolecular(allele.WmdaLocus);
            var serologyName = allele.Name.Split(':')[0].TrimStart('0');
            return new HlaTyping(serologyLocus, serologyName);
        }

        private static IEnumerable<SerologyAssignment> GetSerologyAssignmentsForAlleleIfExists(
            IEnumerable<RelDnaSer> dnaToSerologyRelationships,
            IWmdaHlaTyping allele)
        {
            var relationshipForAllele = dnaToSerologyRelationships.SingleOrDefault(r =>
                r.WmdaLocus.Equals(allele.WmdaLocus) && r.Name.Equals(allele.Name));

            return relationshipForAllele != null ? 
                relationshipForAllele.Assignments : new List<SerologyAssignment>();
        }

        private static IList<DnaToSerologyMapping> GetMappingInfoFromDnaToSerologyRelationships(
            IList<ISerologyInfoForMatching> serologiesInfo,
            IEnumerable<SerologyAssignment> assignmentsForAllele,
            HlaTyping alleleFamilyAsTyping)
        {
            var expectedMatchingSerology = serologiesInfo
                    .FirstOrDefault(m => m.HlaTyping.Equals(alleleFamilyAsTyping))
                    ?.MatchingSerologies;

            return (
                from serology in serologiesInfo
                join assigned in assignmentsForAllele
                    on serology.TypingUsedInMatching.Name equals assigned.Name
                where serology.TypingUsedInMatching.MatchLocus.Equals(alleleFamilyAsTyping.MatchLocus)
                select new DnaToSerologyMapping(
                    (SerologyTyping)serology.HlaTyping,
                    assigned.Assignment,
                    serology.MatchingSerologies.Select(actualMatchingSerology =>
                        GetSerologyMatchInfo(actualMatchingSerology, alleleFamilyAsTyping, expectedMatchingSerology))
                )).ToList();
        }

        private static DnaToSerologyMapping CreateMappingFromAlleleFamily(IWmdaHlaTyping alleleFamilyAsTyping)
        {
            var newSerology = new SerologyTyping(alleleFamilyAsTyping, SerologySubtype.NotSerologyTyping);
            return new DnaToSerologyMapping(
                newSerology,
                Assignment.None,
                new List<DnaToSerologyMatch> { new DnaToSerologyMatch(newSerology) }
            );
        }

        private static DnaToSerologyMatch GetSerologyMatchInfo(
            SerologyTyping actualMatchingSerology,
            HlaTyping alleleFamilyAsTyping,
            IEnumerable<SerologyTyping> expectedMatchingSerology
        )
        {
            var matchInfo = new DnaToSerologyMatch(actualMatchingSerology);

            if (actualMatchingSerology.IsDeleted 
                || UnexpectedDnaToSerologyMappings.PermittedExceptions.Contains(alleleFamilyAsTyping))
                return matchInfo;

            matchInfo.IsUnexpected =
                expectedMatchingSerology == null 
                || !expectedMatchingSerology.Contains(actualMatchingSerology);

            return matchInfo;
        }

        private static bool IsAlleleFamilyInvalidSerology(
            IEnumerable<ISerologyInfoForMatching> serologyInfoForMatching,
            HlaTyping allele,
            HlaTyping alleleFamilyAsTyping)
        {
            var alleleTyping = (AlleleTyping)allele;

            return
                !alleleTyping.IsDeleted
                && !alleleTyping.IsNullExpresser
                && !serologyInfoForMatching.Any(s => s.HlaTyping.Equals(alleleFamilyAsTyping));
        }
    }
}
