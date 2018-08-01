using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
{
    public interface IGradingService
    {
        PhenotypeInfo<MatchGradeResult> CalculateGrades(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults);
    }

    public class GradingService : IGradingService
    {
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

        public PhenotypeInfo<MatchGradeResult> CalculateGrades(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            if (patientLookupResults == null || donorLookupResults == null)
            {
                throw new ArgumentException("Phenotype object cannot be null.");
            }

            return GetPhenotypeGradingResults(patientLookupResults, donorLookupResults);
        }

        private static PhenotypeInfo<MatchGradeResult> GetPhenotypeGradingResults(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            var gradeResults = new PhenotypeInfo<MatchGradeResult>();

            patientLookupResults.EachLocus((locus, patientLookupResult1, patientLookupResult2) =>
            {
                // TODO: NOVA-1301: Score DPB1
                if (locus == Locus.Dpb1)
                {
                    return;
                }

                var patientLookupResultsAtLocus =
                    new Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult>(
                        patientLookupResult1,
                        patientLookupResult2);

                var locusGradeResults = GetLocusGradeResults(
                    patientLookupResultsAtLocus,
                    donorLookupResults.DataAtLocus(locus));

                gradeResults.SetAtLocus(locus, TypePositions.One, locusGradeResults.Result1);
                gradeResults.SetAtLocus(locus, TypePositions.Two, locusGradeResults.Result2);
            });

            return gradeResults;
        }

        private static LocusMatchGradeResults GetLocusGradeResults(
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> patientLookupResults,
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> donorLookupResults)
        {
            var directGrades = GetMatchGradesForDirectOrientation(patientLookupResults, donorLookupResults);
            var crossGrades = GetMatchGradesForCrossOrientation(patientLookupResults, donorLookupResults);

            return GetGradeResultsInBestOrientations(directGrades, crossGrades);
        }

        private static LocusMatchGrades GetMatchGradesForDirectOrientation(
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> patientLookupResults,
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> donorLookupResults)
        {
            var grade1 = CalculateMatchGrade(patientLookupResults.Item1, donorLookupResults.Item1);
            var grade2 = CalculateMatchGrade(patientLookupResults.Item2, donorLookupResults.Item2);

            return new LocusMatchGrades(grade1, grade2);
        }

        private static LocusMatchGrades GetMatchGradesForCrossOrientation(
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> patientLookupResults,
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> donorLookupResults)
        {
            var grade1 = CalculateMatchGrade(patientLookupResults.Item1, donorLookupResults.Item2);
            var grade2 = CalculateMatchGrade(patientLookupResults.Item2, donorLookupResults.Item1);

            return new LocusMatchGrades(grade1, grade2);
        }

        private static MatchGrade CalculateMatchGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult)
        {
            // If either result is missing, default grade is PGroup
            if (patientLookupResult == null || donorLookupResult == null)
            {
                return MatchGrade.PGroup;
            }

            var calculator = GetGradingCalculator(patientLookupResult.HlaScoringInfo, donorLookupResult.HlaScoringInfo);
            return calculator.CalculateGrade(patientLookupResult, donorLookupResult);
        }

        private static GradingCalculatorBase GetGradingCalculator(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo
        )
        {
            // order of checks is critical to which calculator is returned

            if (patientInfo is SerologyScoringInfo || donorInfo is SerologyScoringInfo)
            {
                return new SerologyGradingCalculator();
            }
            else if (patientInfo is ConsolidatedMolecularScoringInfo || donorInfo is ConsolidatedMolecularScoringInfo)
            {
                return new ConsolidatedMolecularGradingCalculator();
            }
            else if (patientInfo is MultipleAlleleScoringInfo || donorInfo is MultipleAlleleScoringInfo)
            {
                return new MultipleAlleleGradingCalculator();
            }

            return new SingleAlleleGradingCalculator();
        }

        private static LocusMatchGradeResults GetGradeResultsInBestOrientations(
            LocusMatchGrades directResults,
            LocusMatchGrades crossResults)
        {
            var bestOrientations = CalculateBestOrientations(directResults, crossResults).ToList();

            var result1 = GetBestMatchGradeResult(bestOrientations, directResults.Grade1, crossResults.Grade1);
            var result2 = GetBestMatchGradeResult(bestOrientations, directResults.Grade2, crossResults.Grade2);

            // Set the higher value grade in position 1
            return (int) result1.GradeResult > (int) result2.GradeResult
                ? new LocusMatchGradeResults(result1, result2)
                : new LocusMatchGradeResults(result2, result1);
        }

        private static IEnumerable<MatchOrientation> CalculateBestOrientations(
            LocusMatchGrades directGrades,
            LocusMatchGrades crossGrades)
        {
            var difference = SumGrades(directGrades) - SumGrades(crossGrades);

            if (difference > 0)
            {
                return new[] { MatchOrientation.Direct };
            }

            if (difference < 0)
            {
                return new[] { MatchOrientation.Cross };
            }

            return new[] { MatchOrientation.Direct, MatchOrientation.Cross };
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
            var crossIsBest = bestOrientations.SequenceEqual(new[] { MatchOrientation.Cross });

            // only use cross if it has been deemed to be the better orientation;
            // else use direct where it is better or both orientations give equally good grades.
            var gradeResult = crossIsBest ? crossGrade : directGrade;

            return new MatchGradeResult(gradeResult, bestOrientations);
        }
    }
}