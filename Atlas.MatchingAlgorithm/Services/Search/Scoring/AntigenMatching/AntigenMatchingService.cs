using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching
{
    public interface IAntigenMatchingService
    {
        PhenotypeInfo<bool?> CalculateAntigenMatches(
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata,
            PhenotypeInfo<MatchGradeResult> matchGrades);
    }

    internal class AntigenMatchingService : IAntigenMatchingService
    {
        private readonly IAntigenMatchCalculator calculator;
        private readonly IScoringCache scoringCache;

        public AntigenMatchingService(IAntigenMatchCalculator calculator, IScoringCache scoringCache)
        {
            this.calculator = calculator;
            this.scoringCache = scoringCache;
        }

        public PhenotypeInfo<bool?> CalculateAntigenMatches(
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata,
            PhenotypeInfo<MatchGradeResult> matchGrades)
        {
            var results = new PhenotypeInfo<bool?>();

            matchGrades.EachLocus((locus, matchGradeResultAtLocus) =>
            {
                var patientLocusData = patientMetadata.GetLocus(locus);
                var donorLocusData = donorMetadata.GetLocus(locus);

                static bool IsLocusInfoNull<T>(LocusInfo<T> locusInfo) => locusInfo is null || locusInfo.Position1And2Null();
                if (IsLocusInfoNull(matchGradeResultAtLocus) || IsLocusInfoNull(patientLocusData) || IsLocusInfoNull(donorLocusData))
                {
                    results.SetLocus(locus, null);
                    return;
                }

                var matchGradesAtLocus = matchGradeResultAtLocus.Map(m => m.GradeResult);

                // need to calculate antigen match according to the orientation provided in grading result
                var orientations = matchGradeResultAtLocus.Position1.Orientations.ToList();
                var locusResults = CalculateLocusAntigenMatchInBestOrientation(orientations, matchGradesAtLocus, patientLocusData, donorLocusData);
                
                results = results.SetLocus(locus, locusResults.Position1, locusResults.Position2);
            });

            return results;
        }

        private LocusInfo<bool?> CalculateLocusAntigenMatchInBestOrientation(
            IReadOnlyCollection<MatchOrientation> orientations,
            LocusInfo<MatchGrade> matchGradesAtLocus,
            LocusInfo<IHlaScoringMetadata> patientLocusData,
            LocusInfo<IHlaScoringMetadata> donorLocusData)
        {
            if (!orientations.Any())
            {
                return new LocusInfo<bool?>();
            }

            if (orientations.Count == 1)
            {
                return CalculateLocusAntigenMatch(orientations.Single(), matchGradesAtLocus, patientLocusData, donorLocusData);
            }

            var directAntigenMatches = CalculateLocusAntigenMatch(MatchOrientation.Direct, matchGradesAtLocus, patientLocusData, donorLocusData);
            var crossAntigenMatches = CalculateLocusAntigenMatch(MatchOrientation.Cross, matchGradesAtLocus, patientLocusData, donorLocusData);

            static int CountAntigenMatches(LocusInfo<bool?> input) => new[] { input.Position1, input.Position2 }.Count(r => r.HasValue && r.Value);
            var directMatchCount = CountAntigenMatches(directAntigenMatches);
            var crossMatchCount = CountAntigenMatches(crossAntigenMatches);

            return directMatchCount >= crossMatchCount ? directAntigenMatches : crossAntigenMatches;
        }

        private LocusInfo<bool?> CalculateLocusAntigenMatch(
            MatchOrientation orientation,
            LocusInfo<MatchGrade> matchGradesAtLocus,
            LocusInfo<IHlaScoringMetadata> patientLocusData,
            LocusInfo<IHlaScoringMetadata> donorLocusData)
        {
            var isGradeOneAntigenMatched = IsAntigenMatch(
                matchGradesAtLocus.Position1, patientLocusData.Position1, orientation == MatchOrientation.Cross ? donorLocusData.Position2 : donorLocusData.Position1);

            var isGradeTwoAntigenMatched = IsAntigenMatch(
                matchGradesAtLocus.Position2, patientLocusData.Position2, orientation == MatchOrientation.Cross ? donorLocusData.Position1 : donorLocusData.Position2);

            return new LocusInfo<bool?>(isGradeOneAntigenMatched, isGradeTwoAntigenMatched);
        }

        private bool? IsAntigenMatch(MatchGrade? matchGrade, IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            return scoringCache.GetOrAddIsAntigenMatch(
                patientMetadata?.Locus,
                patientMetadata?.LookupName,
                donorMetadata?.LookupName,
                c => calculator.IsAntigenMatch(matchGrade, patientMetadata, donorMetadata));
        }
    }
}