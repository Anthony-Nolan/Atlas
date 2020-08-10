using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.DonorImport.Data.Models
{
    [Table("DonorLogs")]
    public class DonorLog
    {
        [Required]
        public string ExternalDonorId { get; set; }
        [Required]
        public DateTime LastUpdateDateTime { get; set; }
    }
    
    public static class DonorLogModelBuilder
    {
        public static void SetUpDonorLogModel(this EntityTypeBuilder<DonorLog> donorModel)
        {
            donorModel.HasKey(d => d.ExternalDonorId);
            donorModel.HasIndex(d => d.ExternalDonorId).IsUnique();
        }
    }
}