﻿using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading
{
    public interface IGradingService
    {
        PhenotypeInfo<MatchGradeResult> CalculateGrades(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults);
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

        private const MatchGrade DefaultMatchGradeForUntypedLocus = MatchGrade.PGroup;
        private readonly IPermissiveMismatchCalculator permissiveMismatchCalculator;

        public GradingService(IPermissiveMismatchCalculator permissiveMismatchCalculator, IScoringCache scoringCache)
        {
            this.permissiveMismatchCalculator = permissiveMismatchCalculator;
            this.scoringCache = scoringCache;
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

        private PhenotypeInfo<MatchGradeResult> GetPhenotypeGradingResults(
            PhenotypeInfo<IHlaScoringLookupResult> patientLookupResults,
            PhenotypeInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            var gradeResults = new PhenotypeInfo<MatchGradeResult>();

            patientLookupResults.EachLocus((locus, patientLookupResult1, patientLookupResult2) =>
            {
                var patientLookupResultsAtLocus = new LocusInfo<IHlaScoringLookupResult>(patientLookupResult1, patientLookupResult2);
                var donorLookupResultsAtLocus = donorLookupResults.GetLocus(locus);

                var locusGradeResults = GetLocusGradeResults(patientLookupResultsAtLocus, donorLookupResultsAtLocus);

                gradeResults.SetPosition(locus, LocusPosition.One, locusGradeResults.Result1);
                gradeResults.SetPosition(locus, LocusPosition.Two, locusGradeResults.Result2);
            });

            return gradeResults;
        }

        private LocusMatchGradeResults GetLocusGradeResults(
            LocusInfo<IHlaScoringLookupResult> patientLookupResults,
            LocusInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            var directGrades = GetMatchGradesForDirectOrientation(patientLookupResults, donorLookupResults);
            var crossGrades = GetMatchGradesForCrossOrientation(patientLookupResults, donorLookupResults);

            return GetGradeResultsInBestOrientations(directGrades, crossGrades);
        }

        private LocusMatchGrades GetMatchGradesForDirectOrientation(
            LocusInfo<IHlaScoringLookupResult> patientLookupResults,
            LocusInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            var grade1 = CalculateMatchGrade(patientLookupResults.Position1, donorLookupResults.Position1);
            var grade2 = CalculateMatchGrade(patientLookupResults.Position2, donorLookupResults.Position2);

            return new LocusMatchGrades(grade1, grade2);
        }

        private LocusMatchGrades GetMatchGradesForCrossOrientation(
            LocusInfo<IHlaScoringLookupResult> patientLookupResults,
            LocusInfo<IHlaScoringLookupResult> donorLookupResults)
        {
            var grade1 = CalculateMatchGrade(patientLookupResults.Position1, donorLookupResults.Position2);
            var grade2 = CalculateMatchGrade(patientLookupResults.Position2, donorLookupResults.Position1);

            return new LocusMatchGrades(grade1, grade2);
        }

        private MatchGrade CalculateMatchGrade(
            IHlaScoringLookupResult patientLookupResult,
            IHlaScoringLookupResult donorLookupResult)
        {
            if (patientLookupResult == null || donorLookupResult == null)
            {
                return DefaultMatchGradeForUntypedLocus;
            }

            return scoringCache.GetOrAddMatchGrade(patientLookupResult.Locus, patientLookupResult.LookupName, donorLookupResult.LookupName,
                c =>
                {
                    var calculator = GradingCalculatorFactory.GetGradingCalculator(
                        permissiveMismatchCalculator,
                        patientLookupResult.HlaScoringInfo,
                        donorLookupResult.HlaScoringInfo);
                    var grade = calculator.CalculateGrade(patientLookupResult, donorLookupResult);
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