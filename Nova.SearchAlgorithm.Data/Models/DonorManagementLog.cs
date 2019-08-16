using System;
using System.ComponentModel.DataAnnotations;

namespace Nova.SearchAlgorithm.Data.Models
{
    public class DonorManagementLog
    {
        public int Id { get; set; }

        [Required]
        public int DonorId { get; set; }

        [Required]
        public long SequenceNumberOfLastUpdate { get; set; }

        [Required]
        public DateTimeOffset LastUpdateDateTime { get; set; }
    }
}