using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;

namespace Nova.SearchAlgorithm.Services.Scoring
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
            // TODO: NOVA-1446: Implement
            return new PhenotypeInfo<MatchGradeResult>
            {
                A_1 = new MatchGradeResult
                {
                    GradeResult = MatchGrade.Split,
                    Orientations = new []{MatchOrientation.Cross, MatchOrientation.Direct}
                },
                A_2 = new MatchGradeResult
                {
                    GradeResult = MatchGrade.Split,
                    Orientations = new []{MatchOrientation.Cross, MatchOrientation.Direct}
                },
                B_1 = new MatchGradeResult
                {
                    GradeResult = MatchGrade.Split,
                    Orientations = new []{MatchOrientation.Cross, MatchOrientation.Direct}
                },
                B_2= new MatchGradeResult
                {
                    GradeResult = MatchGrade.Split,
                    Orientations = new []{MatchOrientation.Cross, MatchOrientation.Direct}
                },
                C_1 = new MatchGradeResult
                {
                    GradeResult = MatchGrade.Split,
                    Orientations = new []{MatchOrientation.Cross, MatchOrientation.Direct}
                },
                C_2 = new MatchGradeResult
                {
                    GradeResult = MatchGrade.Split,
                    Orientations = new []{MatchOrientation.Cross, MatchOrientation.Direct}
                },
                DQB1_1 = new MatchGradeResult
                {
                    GradeResult = MatchGrade.Split,
                    Orientations = new []{MatchOrientation.Cross, MatchOrientation.Direct}
                },
                DQB1_2 = new MatchGradeResult
                {
                    GradeResult = MatchGrade.Split,
                    Orientations = new []{MatchOrientation.Cross, MatchOrientation.Direct}
                },
                DRB1_1 = new MatchGradeResult
                {
                    GradeResult = MatchGrade.Split,
                    Orientations = new []{MatchOrientation.Cross, MatchOrientation.Direct}
                },
                DRB1_2 = new MatchGradeResult
                {
                    GradeResult = MatchGrade.Split,
                    Orientations = new []{MatchOrientation.Cross, MatchOrientation.Direct}
                },
            };
        }
    }
}