using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Data.Models
{
    public class HaplotypeFrequency
    {
        public long Id { get; set; }

        [ForeignKey("Set_Id")]
        public HaplotypeFrequencySet SetId { get; set; }

        public decimal Frequency { get; set; }

        [Required]
        [MaxLength(64)]
        public string A { get; set; }

        [Required]
        [MaxLength(64)]
        public string B { get; set; }

        [Required]
        [MaxLength(64)]
        public string C { get; set; }

        [Required]
        [MaxLength(64)]
        public string DQB1 { get; set; }

        [Required]
        [MaxLength(64)]
        public string DRB1 { get; set; }
    }
}