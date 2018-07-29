using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;
using System.Collections.Generic;
using System.Linq;

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
                new[] { SerologySubtype.Broad },
                new[] { SerologySubtype.Split, SerologySubtype.Associated }))
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
            var patientDirect = GetDirectlyMappedSerologies(patientSerologies);
            var donorDirect = GetDirectlyMappedSerologies(donorSerologies);

            return patientDirect
                .Intersect(donorDirect)
                .Any(x => x.Item2 == matchingSubtype);
        }

        private static bool IsIndirectMatch(
            IReadOnlyCollection<SerologyEntry> patientSerologies,
            IReadOnlyCollection<SerologyEntry> donorSerologies,
            IEnumerable<SerologySubtype> firstSetOfSubtypes,
            IReadOnlyCollection<SerologySubtype> secondSetOfSubtypes)
        {
            var subtypePairs = firstSetOfSubtypes
                .SelectMany(directSer => secondSetOfSubtypes, GetSubtypePairs)
                .SelectMany(subtypePair => subtypePair)
                .ToList();

            return
                subtypePairs.Any(subtypePair => IsIndirectSerologySubtypeMatch(
                    patientSerologies,
                    donorSerologies,
                    subtypePair.Item1,
                    subtypePair.Item2)) ||
                subtypePairs.Any(pairing => IsIndirectSerologySubtypeMatch(
                    donorSerologies,
                    patientSerologies,
                    pairing.Item1,
                    pairing.Item2));
        }

        private static IEnumerable<Tuple<SerologySubtype, SerologySubtype>> GetSubtypePairs(
            SerologySubtype firstSubtype,
            SerologySubtype secondSubtype)
        {
            var firstPair = new Tuple<SerologySubtype, SerologySubtype>(firstSubtype, secondSubtype);
            var secondPair = new Tuple<SerologySubtype, SerologySubtype>(secondSubtype, firstSubtype);

            return new[] { firstPair, secondPair };
        }

        private static bool IsIndirectSerologySubtypeMatch(
            IReadOnlyCollection<SerologyEntry> firstTypingSerologies,
            IEnumerable<SerologyEntry> secondTypingSerologies,
            SerologySubtype directMatchingSubtype,
            SerologySubtype indirectMatchingSubtype)
        {
            var firstTypingDirect = GetDirectlyMappedSerologies(firstTypingSerologies);
            var firstTypingIndirect = GetIndirectlyMappedSerologies(firstTypingSerologies);
            var secondTypingDirect = GetDirectlyMappedSerologies(secondTypingSerologies);

            return firstTypingDirect.Any(serology => serology.Item2 == directMatchingSubtype) &&
                firstTypingIndirect
                    .Intersect(secondTypingDirect)
                    .Any(serology => serology.Item2 == indirectMatchingSubtype);
        }

        private static IEnumerable<Tuple<string, SerologySubtype>> GetDirectlyMappedSerologies(
            IEnumerable<SerologyEntry> serologies)
        {
            return serologies
                .Where(s => s.IsDirectMapping)
                .Select(s => new Tuple<string, SerologySubtype>(s.Name, s.SerologySubtype));
        }

        private static IEnumerable<Tuple<string, SerologySubtype>> GetIndirectlyMappedSerologies(
            IEnumerable<SerologyEntry> serologies)
        {
            return serologies
                .Where(s => !s.IsDirectMapping)
                .Select(s => new Tuple<string, SerologySubtype>(s.Name, s.SerologySubtype));
        }
    }
}