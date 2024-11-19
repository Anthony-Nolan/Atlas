using System.ComponentModel.DataAnnotations;
using Atlas.Client.Models.Common.Results;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Models.ScoringWeightings
{
    public class ScoringWeighting
    {
        
        public int Id { get; set; }
        
        /// <summary>
        /// The name corresponding to an enum value (e.g. Grade, Confidence) in the codebase.
        /// This must match the values represented by the appropriate enum, of <see cref="MatchGrade"/> and <see cref="MatchConfidence"/>
        /// </summary>
        [StringLength(100)]
        public string Name { get; set; }
        
        /// <summary>
        /// An integer weight used for relative ordering of grades/confidences in results
        /// </summary>
        public int Weight { get; set; }
    }
}