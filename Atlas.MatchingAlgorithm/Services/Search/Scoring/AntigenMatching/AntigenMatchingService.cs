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

                if (IsLocusInfoNull(matchGradeResultAtLocus) || IsLocusInfoNull(patientLocusData) || IsLocusInfoNull(donorLocusData))
                {
                    results.SetLocus(locus, null);
                    return;
                }

                static bool IsLocusInfoNull<T>(LocusInfo<T> locusInfo) => locusInfo is null || locusInfo.Position1And2Null();

                var matchGradesAtLocus = matchGradeResultAtLocus.Map(m => m.GradeResult);

                // need to calculate antigen match according to the orientation provided in grading result
                var orientations = matchGradeResultAtLocus.Position1.Orientations.ToList();
                var isCrossBest = orientations.Count == 1 && orientations.Single() == MatchOrientation.Cross;

                var isGradeOneAntigenMatched = IsAntigenMatch(
                    matchGradesAtLocus.Position1, patientLocusData.Position1, isCrossBest ? donorLocusData.Position2 : donorLocusData.Position1);

                var isGradeTwoAntigenMatched = IsAntigenMatch(
                    matchGradesAtLocus.Position2, patientLocusData.Position2, isCrossBest ? donorLocusData.Position1 : donorLocusData.Position2);

                results = results.SetLocus(locus, isGradeOneAntigenMatched, isGradeTwoAntigenMatched);
            });

            return results;
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