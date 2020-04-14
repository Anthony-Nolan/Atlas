using System;
using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
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