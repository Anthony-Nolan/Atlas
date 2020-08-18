using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
{
    /// <summary>
    /// To be used when at least one typing is serology;
    /// the other typing can be either serology or molecular.
    /// Grades definitions have been taken from the WMDA Matching Framework (2010).
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
            IHlaScoringMetadata patientMetadata,
            IHlaScoringMetadata donorMetadata)
        {
            var patientSerologies = patientMetadata.HlaScoringInfo.MatchingSerologies.ToList();
            var donorSerologies = donorMetadata.HlaScoringInfo.MatchingSerologies.ToList();

            // Order of the following checks is critical to the grade outcome

            if (IsAssociatedMatch(patientSerologies, donorSerologies))
            {
                return MatchGrade.Associated;
            }
            else if (IsSplitMatch(patientSerologies, donorSerologies))
            {
                return MatchGrade.Split;
            }
            else if (IsBroadMatch(patientSerologies, donorSerologies))
            {
                return MatchGrade.Broad;
            }

            return MatchGrade.Mismatch;
        }

        /// <summary>
        /// Are both serologies the same, and of the Associated subtype?
        /// </summary>
        private static bool IsAssociatedMatch(
            IEnumerable<SerologyEntry> patientSerologies,
            IEnumerable<SerologyEntry> donorSerologies)
        {
            return IsDirectMatch(
                patientSerologies,
                donorSerologies,
                SerologySubtype.Associated);
        }

        /// <summary>
        /// Are both serologies the same, and of the Split subtype? OR
        /// Are both serologies the same, and of the Not-Split subtype? OR
        /// Is one serology Not-Split & the other Associated to it (or vice versa)?
        /// </summary>
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
                new[] {SerologySubtype.Split, SerologySubtype.NotSplit},
                new[] {SerologySubtype.Associated}))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Are both serologies the same, and of the Broad subtype? OR
        /// Is one serology a Broad & the other a Split of it (or vice versa)? OR
        /// Is one serology a Broad & the other an Associated to one of its Splits (or vice versa)?
        /// </summary>
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
                new[] {SerologySubtype.Broad},
                new[] {SerologySubtype.Split, SerologySubtype.Associated}))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Do patient & donor HLA have the same directly mapped serology?
        /// </summary>
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

        /// <summary>
        /// Do patient & donor HLA have an indirect matching serology relationship?
        /// </summary>
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

            var isPatientVsDonorMatch = subtypePairs.Any(subtypePair =>
                IsIndirectSerologySubtypeMatch(
                    patientSerologies,
                    donorSerologies,
                    subtypePair.Position1,
                    subtypePair.Position2));

            var isDonorVsPatientMatch = subtypePairs.Any(subtypePair =>
                IsIndirectSerologySubtypeMatch(
                    donorSerologies,
                    patientSerologies,
                    subtypePair.Position1,
                    subtypePair.Position2));

            return isPatientVsDonorMatch || isDonorVsPatientMatch;
        }

        private static IEnumerable<LocusInfo<SerologySubtype>> GetSubtypePairs(
            SerologySubtype firstSubtype,
            SerologySubtype secondSubtype)
        {
            var firstPair = new LocusInfo<SerologySubtype>(firstSubtype, secondSubtype);
            var secondPair = new LocusInfo<SerologySubtype>(secondSubtype, firstSubtype);

            return new[] {firstPair, secondPair};
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