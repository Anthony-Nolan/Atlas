using System.Collections.Generic;
using System.Linq;
using System.Web.DynamicData;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    /// <summary>
    /// To be used when at least one typing is serology;
    /// the other typing can be either serology or molecular.
    /// </summary>
    public interface ISerologyGradingCalculator : IGradingCalculator
    {
    }

    public class SerologyGradingCalculator :
        GradingCalculatorBase,
        ISerologyGradingCalculator
    {
        protected override bool ScoringInfosAreOfPermittedTypes(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo)
        {
            return patientInfo is SerologyScoringInfo ||
                   donorInfo is SerologyScoringInfo;
        }

        protected override MatchGrade GetMatchGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult)
        {
            var patientSerologies = patientLookupResult.HlaScoringInfo.MatchingSerologies.ToList();
            var donorSerologies = donorLookupResult.HlaScoringInfo.MatchingSerologies.ToList();

            // Order of the following checks is critical to the grade outcome

            if (IsAssociatedMatch(patientSerologies, donorSerologies))
            {
                return MatchGrade.Associated;
            }

            if (IsSplitMatch(patientSerologies, donorSerologies))
            {
                return MatchGrade.Split;
            }

            if (IsBroadMatch(patientSerologies, donorSerologies))
            {
                return MatchGrade.Broad;
            }

            return MatchGrade.Mismatch;
        }

        private static bool IsAssociatedMatch(
            IEnumerable<SerologyEntry> patientSerologies,
            IEnumerable<SerologyEntry> donorSerologies)
        {
            return IsDirectMatch(
                patientSerologies,
                donorSerologies,
                SerologySubtype.Associated);
        }

        private static bool IsSplitMatch(
            IReadOnlyCollection<SerologyEntry> patientSerologies,
            IReadOnlyCollection<SerologyEntry> donorSerologies)
        {
            if (IsDirectMatch(patientSerologies, donorSerologies, SerologySubtype.Split))
            {
                return true;
            }

            if (IsDirectMatch(patientSerologies, donorSerologies, SerologySubtype.NotSplit))
            {
                return true;
            }

            if (IsIndirectMatch(
                patientSerologies,
                donorSerologies,
                new[] { SerologySubtype.Split, SerologySubtype.NotSplit },
                new[] { SerologySubtype.Associated }))
            {
                return true;
            }

            return false;
        }

        private static bool IsBroadMatch(
            IReadOnlyCollection<SerologyEntry> patientSerologies,
            IReadOnlyCollection<SerologyEntry> donorSerologies)
        {
            if (IsDirectMatch(patientSerologies, donorSerologies, SerologySubtype.Broad))
            {
                return true;
            }

            if (IsIndirectMatch(
                patientSerologies,
                donorSerologies,
                new[] { SerologySubtype.Split, SerologySubtype.Associated },
                new[] { SerologySubtype.Broad}))
            {
                return true;
            }

            return false;
        }

        private static bool IsDirectMatch(
            IEnumerable<SerologyEntry> patientSerologies,
            IEnumerable<SerologyEntry> donorSerologies,
            SerologySubtype matchingSubtype)
        {
            var patientDirect = GetDirectlyMappedSerology(patientSerologies);
            var donorDirect = GetDirectlyMappedSerology(donorSerologies);

            return patientDirect
                .Intersect(donorDirect)
                .Any(x => x.SerologySubtype == matchingSubtype);
        }

        private static bool IsIndirectMatch(
            IReadOnlyCollection<SerologyEntry> patientSerologies,
            IReadOnlyCollection<SerologyEntry> donorSerologies,
            IReadOnlyCollection<SerologySubtype> firstSetOfSubtypes,
            IReadOnlyCollection<SerologySubtype> secondSetOfSubtypes)
        {
            var subtypePairings =
                firstSetOfSubtypes
                    .SelectMany(directSer => secondSetOfSubtypes,
                        (firstSubtype, secondSubtype) => new { firstSubtype, secondSubtype })
                        .Concat(secondSetOfSubtypes
                            .SelectMany(directSer => firstSetOfSubtypes,
                                (firstSubtype, secondSubtype) => new { firstSubtype, secondSubtype }))
                                    .ToList();

            return
                subtypePairings.Any(pairing => IsIndirectSerologySubtypeMatch(
                    patientSerologies,
                    donorSerologies,
                    pairing.firstSubtype,
                    pairing.secondSubtype)) ||
                subtypePairings.Any(pairing => IsIndirectSerologySubtypeMatch(
                    donorSerologies,
                    patientSerologies,
                    pairing.firstSubtype,
                    pairing.secondSubtype));
        }

        private static bool IsIndirectSerologySubtypeMatch(
            IReadOnlyCollection<SerologyEntry> firstTypingSerologies,
            IEnumerable<SerologyEntry> secondTypingSerologies,
            SerologySubtype directMatchingSubtype,
            SerologySubtype indirectMatchingSubtype)
        {
            var firstTypingDirect = GetDirectlyMappedSerology(firstTypingSerologies);
            var firstTypingIndirect = GetIndirectlyMappedSerology(firstTypingSerologies);

            var secondTypingDirect = GetDirectlyMappedSerology(secondTypingSerologies);

            return firstTypingDirect.Any(ser => ser.SerologySubtype == directMatchingSubtype) &&
                firstTypingIndirect.Join(secondTypingDirect,
                        firstIndirect => new { firstIndirect.Name, firstIndirect.SerologySubtype },
                        secondDirect => new { secondDirect.Name, secondDirect.SerologySubtype },
                        (firstIndirect, secondDirect) => new { firstIndirect, secondDirect })
                    .Any(ser => ser.firstIndirect.SerologySubtype == indirectMatchingSubtype);
        }

        private static IEnumerable<SerologyEntry> GetDirectlyMappedSerology(
            IEnumerable<SerologyEntry> serologies)
        {
            return serologies.Where(s => s.IsDirectMapping);
        }

        private static IEnumerable<SerologyEntry> GetIndirectlyMappedSerology(
            IEnumerable<SerologyEntry> serologies)
        {
            return serologies.Where(s => !s.IsDirectMapping);
        }
    }
}