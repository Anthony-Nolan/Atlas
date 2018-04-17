using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nova.SearchAlgorithm.Data.Entity
{
    public class DonorHla
    {
        [Key]
        [Column(Order = 1)]
        public int DonorId { get; set; }
        [Key]
        [Column(Order = 2)]
        public string Locus { get; set; }
        public int TypePosition { get; set; }
        public string HlaName { get; set; }
    }
}