using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Models.ScoringWeightings
{
    public class ScoringWeighting
    {
        
        public int Id { get; set; }
        
        /// <summary>
        /// The name corresponding to an enum value (e.g. Grade, Confidence) in the codebase
        /// </summary>
        [StringLength(100)]
        public string Name { get; set; }
        
        /// <summary>
        /// An integer weight used for relative ordering of grades/confidences in results
        /// </summary>
        public int Weight { get; set; }
    }
}