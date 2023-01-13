using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading
{
    public interface IGradingService
    {
        PhenotypeInfo<MatchGradeResult> CalculateGrades(
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata);
    }

    public class GradingService : IGradingService
    {
        private readonly IScoringCache scoringCache;

        private class LocusMatchGrades
        {
            public MatchGrade Grade1 { get; }
            public MatchGrade Grade2 { get; }

            public LocusMatchGrades(MatchGrade grade1, MatchGrade grade2)
            {
                Grade1 = grade1;
                Grade2 = grade2;
            }
        }

        private class LocusMatchGradeResults
        {
            public MatchGradeResult Result1 { get; }
            public MatchGradeResult Result2 { get; }

            public LocusMatchGradeResults(MatchGradeResult result1, MatchGradeResult result2)
            {
                Result1 = result1;
                Result2 = result2;
            }
        }

        private readonly LociInfo<MatchGrade> defaultMatchGradeForUntypedLocus =
            new LociInfo<MatchGrade>(
                MatchGrade.PGroup,
                MatchGrade.PGroup,
                MatchGrade.PGroup,
                // Due to the unique nature of DPB1 allowing permissive mismatches, it is configured to treat untyped loci as "unknown".
                // All other loci consider untyped loci as "potential matches", as the HLA has the possibility of being a match.
                MatchGrade.Unknown,
                MatchGrade.PGroup,
                MatchGrade.PGroup
            );

        public GradingService(IScoringCache scoringCache)
        {
            this.scoringCache = scoringCache;
        }

        public PhenotypeInfo<MatchGradeResult> CalculateGrades(
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata)
        {
            if (patientMetadata == null || donorMetadata == null)
            {
                throw new ArgumentException("Phenotype object cannot be null.");
            }

            return GetPhenotypeGradingResults(patientMetadata, donorMetadata);
        }

        private PhenotypeInfo<MatchGradeResult> GetPhenotypeGradingResults(
            PhenotypeInfo<IHlaScoringMetadata> patientMetadata,
            PhenotypeInfo<IHlaScoringMetadata> donorMetadata)
        {
            var gradeResults = new PhenotypeInfo<MatchGradeResult>();

            patientMetadata.EachLocus((locus, patientMetadataAtLocus) =>
            {
                var donorMetadataAtLocus = donorMetadata.GetLocus(locus);

                var locusGradeResults = GetLocusGradeResults(patientMetadataAtLocus, donorMetadataAtLocus, locus);

                gradeResults = gradeResults
                    .SetPosition(locus, LocusPosition.One, locusGradeResults.Result1)
                    .SetPosition(locus, LocusPosition.Two, locusGradeResults.Result2);
            });

            return gradeResults;
        }

        private LocusMatchGradeResults GetLocusGradeResults(
            LocusInfo<IHlaScoringMetadata> patientMetadata,
            LocusInfo<IHlaScoringMetadata> donorMetadata,
            Locus locus)
        {
            var directGrades = GetMatchGradesForDirectOrientation(patientMetadata, donorMetadata, locus);
            var crossGrades = GetMatchGradesForCrossOrientation(patientMetadata, donorMetadata, locus);

            return GetGradeResultsInBestOrientations(directGrades, crossGrades);
        }

        private LocusMatchGrades GetMatchGradesForDirectOrientation(
            LocusInfo<IHlaScoringMetadata> patientMetadata,
            LocusInfo<IHlaScoringMetadata> donorMetadata,
            Locus locus)
        {
            var grade1 = CalculateMatchGrade(patientMetadata.Position1, donorMetadata.Position1, locus);
            var grade2 = CalculateMatchGrade(patientMetadata.Position2, donorMetadata.Position2, locus);

            return new LocusMatchGrades(grade1, grade2);
        }

        private LocusMatchGrades GetMatchGradesForCrossOrientation(
            LocusInfo<IHlaScoringMetadata> patientMetadata,
            LocusInfo<IHlaScoringMetadata> donorMetadata,
            Locus locus)
        {
            var grade1 = CalculateMatchGrade(patientMetadata.Position1, donorMetadata.Position2, locus);
            var grade2 = CalculateMatchGrade(patientMetadata.Position2, donorMetadata.Position1, locus);

            return new LocusMatchGrades(grade1, grade2);
        }

        private MatchGrade CalculateMatchGrade(
            IHlaScoringMetadata patientMetadata,
            IHlaScoringMetadata donorMetadata,
            Locus locus)
        {
            if (patientMetadata == null || donorMetadata == null)
            {
                return defaultMatchGradeForUntypedLocus.GetLocus(locus);
            }

            return scoringCache.GetOrAddMatchGrade(patientMetadata.Locus, patientMetadata.LookupName, donorMetadata.LookupName,
                c =>
                {
                    var calculator = GradingCalculatorFactory.GetGradingCalculator(
                        patientMetadata.HlaScoringInfo,
                        donorMetadata.HlaScoringInfo);
                    var grade = calculator.CalculateGrade(patientMetadata, donorMetadata);
                    return grade;
                });
        }

        private static LocusMatchGradeResults GetGradeResultsInBestOrientations(
            LocusMatchGrades directResults,
            LocusMatchGrades crossResults)
        {
            var bestOrientations = CalculateBestOrientations(directResults, crossResults).ToList();

            var result1 = GetBestMatchGradeResult(bestOrientations, directResults.Grade1, crossResults.Grade1);
            var result2 = GetBestMatchGradeResult(bestOrientations, directResults.Grade2, crossResults.Grade2);

            return new LocusMatchGradeResults(result1, result2);
        }

        private static IEnumerable<MatchOrientation> CalculateBestOrientations(
            LocusMatchGrades directGrades,
            LocusMatchGrades crossGrades)
        {
            var difference = SumGrades(directGrades) - SumGrades(crossGrades);

            if (difference > 0)
            {
                return new[] {MatchOrientation.Direct};
            }

            if (difference < 0)
            {
                return new[] {MatchOrientation.Cross};
            }

            return new[] {MatchOrientation.Direct, MatchOrientation.Cross};
        }

        private static int SumGrades(LocusMatchGrades grades)
        {
            return (int) grades.Grade1 + (int) grades.Grade2;
        }

        private static MatchGradeResult GetBestMatchGradeResult(
            IEnumerable<MatchOrientation> bestOrientations,
            MatchGrade directGrade,
            MatchGrade crossGrade)
        {
            var crossIsBest = bestOrientations.SequenceEqual(new[] {MatchOrientation.Cross});

            // only use cross if it has been deemed to be the better orientation;
            // else use direct where it is better or both orientations give equally good grades.
            var gradeResult = crossIsBest ? crossGrade : directGrade;

            return new MatchGradeResult(gradeResult, bestOrientations);
        }
    }
}