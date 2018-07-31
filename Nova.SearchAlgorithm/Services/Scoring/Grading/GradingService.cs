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
        public PhenotypeInfo<MatchGradeResult> CalculateGrades(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            if (patientLookupResults == null || donorLookupResults == null)
            {
                throw new ArgumentException("HLA scoring lookup result cannot be null.");
            }

            return GetPhenotypeGradingResults(patientLookupResults, donorLookupResults);
        }

        private static PhenotypeInfo<MatchGradeResult> GetPhenotypeGradingResults(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            var phenotype = new PhenotypeInfo<MatchGradeResult>();

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

                phenotype.SetAtLocus(locus, TypePositions.One, locusGradeResults.Item1);
                phenotype.SetAtLocus(locus, TypePositions.Two, locusGradeResults.Item2);
            });

            return phenotype;
        }

        private static Tuple<MatchGradeResult, MatchGradeResult> GetLocusGradeResults(
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> patientLookupResults,
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> donorLookupResults)
        {
            var directGrades = GetMatchGradesForDirectOrientation(patientLookupResults, donorLookupResults);
            var crossGrades = GetMatchGradesForCrossOrientation(patientLookupResults, donorLookupResults);

            return GetGradeResultsInBestOrientations(directGrades, crossGrades);
        }

        private static Tuple<MatchGrade, MatchGrade> GetMatchGradesForDirectOrientation(
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> patientLookupResults,
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> donorLookupResults)
        {
            var grade1 = CalculateMatchGrade(patientLookupResults.Item1, donorLookupResults.Item1);
            var grade2 = CalculateMatchGrade(patientLookupResults.Item2, donorLookupResults.Item2);

            return new Tuple<MatchGrade, MatchGrade>(grade1, grade2);
        }

        private static Tuple<MatchGrade, MatchGrade> GetMatchGradesForCrossOrientation(
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> patientLookupResults,
            Tuple<IHlaScoringLookupResult, IHlaScoringLookupResult> donorLookupResults)
        {
            var grade1 = CalculateMatchGrade(patientLookupResults.Item1, donorLookupResults.Item2);
            var grade2 = CalculateMatchGrade(patientLookupResults.Item2, donorLookupResults.Item1);

            return new Tuple<MatchGrade, MatchGrade>(grade1, grade2);
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

            return new AlleleGradingCalculator();
        }

        private static Tuple<MatchGradeResult, MatchGradeResult> GetGradeResultsInBestOrientations(
            Tuple<MatchGrade, MatchGrade> directResults,
            Tuple<MatchGrade, MatchGrade> crossResults)
        {
            var bestOrientations = CalculateBestOrientations(directResults, crossResults).ToList();

            var result1 = GetBestMatchGradeResult(bestOrientations, directResults.Item1, crossResults.Item1);
            var result2 = GetBestMatchGradeResult(bestOrientations, directResults.Item2, crossResults.Item2);

            return new Tuple<MatchGradeResult, MatchGradeResult>(result1, result2);
        }

        private static IEnumerable<MatchOrientation> CalculateBestOrientations(
            Tuple<MatchGrade, MatchGrade> directGrades,
            Tuple<MatchGrade, MatchGrade> crossGrades)
        {
            var difference = SumGrades(directGrades) - SumGrades(crossGrades);

            switch (difference)
            {
                case var _ when difference > 0:
                    return new[] { MatchOrientation.Direct };
                case var _ when difference < 0:
                    return new[] { MatchOrientation.Cross };
                default:
                    return new[] { MatchOrientation.Direct, MatchOrientation.Cross };
            }
        }

        private static int SumGrades(Tuple<MatchGrade, MatchGrade> grades)
        {
            return (int)grades.Item1 + (int)grades.Item2;
        }

        private static MatchGradeResult GetBestMatchGradeResult(
            IEnumerable<MatchOrientation> bestOrientations,
            MatchGrade directGrade,
            MatchGrade crossGrade)
        {
            var crossIsBest = bestOrientations.Equals(new[] { MatchOrientation.Cross });
            var gradeResult = crossIsBest ? crossGrade: directGrade;

            return new MatchGradeResult(gradeResult, bestOrientations);
        }
    }
}