using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.MatchPrediction.Data.Models
{
    public class HaplotypeInfo
    {
        public int Id { get; set; }
        [ForeignKey("Set_Id")]
        public HaplotypeFrequencySets SetId { get; set; }
        public decimal Frequency { get; set; }
        [Required]
        public string A { get; set; }
        [Required]
        public string B { get; set; }
        [Required]
        public string C { get; set; }
        [Required]
        public string DQB1 { get; set; }
        [Required]
        public string DRB1 { get; set; }
    }
}
