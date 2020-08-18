using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Models.ScoringWeightings;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Repositories
{
    public interface IScoringWeightingRepository
    {
        int GetGradeWeighting(MatchGrade matchGrade);
        int GetConfidenceWeighting(MatchConfidence matchConfidence);
    }
    
    public class ScoringWeightingRepository : IScoringWeightingRepository
    {
        private readonly IEnumerable<GradeWeighting> gradeWeightings;
        private readonly IEnumerable<ConfidenceWeighting> confidenceWeightings;

        public ScoringWeightingRepository(SearchAlgorithmPersistentContext context)
        {
            gradeWeightings = context.Set<GradeWeighting>().ToList();
            confidenceWeightings = context.Set<ConfidenceWeighting>().ToList();
        }
        
        public int GetGradeWeighting(MatchGrade matchGrade)
        {
            var weighting = gradeWeightings.SingleOrDefault(w => w.Name == matchGrade.ToString());
            if (weighting == null)
            {
                throw new Exception($"Weighting for grade {matchGrade} not found in database");
            }
            return weighting.Weight;
        }

        public int GetConfidenceWeighting(MatchConfidence matchConfidence)
        {
            var weighting = confidenceWeightings.SingleOrDefault(w => w.Name == matchConfidence.ToString());
            if (weighting == null)
            {
                throw new Exception($"Weighting for confidence {matchConfidence} not found in database");
            }
            return weighting.Weight;
        }
    }
}