using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using Atlas.Utils.Models;

namespace Atlas.MatchingAlgorithm.Test.Builders.SearchResults
{
    public class MatchAndScoreResultBuilder
    {
        private MatchResultBuilder matchResultBuilder;
        private ScoreResultBuilder scoreResultBuilder;

        public MatchAndScoreResultBuilder()
        {
            matchResultBuilder = new MatchResultBuilder();
            scoreResultBuilder = new ScoreResultBuilder();
        }

        public MatchAndScoreResultBuilder WithMatchCountAtLocus(Locus locus, int matchCount)
        {
            matchResultBuilder = matchResultBuilder.WithMatchCountAtLocus(locus, matchCount);
            scoreResultBuilder = scoreResultBuilder.WithMatchCountAtLocus(locus, matchCount);
            return this;
        }
        
        public MatchAndScoreResultBuilder WithMatchGradeAtLocus(Locus locus, MatchGrade matchGrade)
        {
            scoreResultBuilder = scoreResultBuilder.WithMatchGradeAtLocus(locus, matchGrade);    
            return this;
        }
        
        public MatchAndScoreResultBuilder WithMatchGradeScoreAtLocus(Locus locus, int matchGradeScore)
        {
            scoreResultBuilder = scoreResultBuilder.WithMatchGradeScoreAtLocus(locus, matchGradeScore);    
            return this;
        }
        
        public MatchAndScoreResultBuilder WithMatchConfidenceAtLocus(Locus locus, MatchConfidence matchConfidence)
        {
            scoreResultBuilder = scoreResultBuilder.WithMatchConfidenceAtLocus(locus, matchConfidence);    
            return this;
        }
        
        public MatchAndScoreResultBuilder WithMatchConfidenceScoreAtLocus(Locus locus, int matchConfidenceScore)
        {
            scoreResultBuilder = scoreResultBuilder.WithMatchConfidenceScoreAtLocus(locus, matchConfidenceScore);    
            return this;
        }

        public MatchAndScoreResultBuilder WithAggregateScoringData(AggregateScoreDetails aggregateScoreDetails)
        {
            scoreResultBuilder = scoreResultBuilder.WithAggregateScoringData(aggregateScoreDetails);
            return this;
        }
        
        public MatchAndScoreResult Build()
        {
            return new MatchAndScoreResult
            {
                MatchResult = matchResultBuilder.Build(),
                ScoreResult = scoreResultBuilder.Build(),
            };
        }
    }
}