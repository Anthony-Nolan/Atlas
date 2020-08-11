using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.DonorImport.Data.Models
{
    public class DonorLog
    {
        [Required]
        public string ExternalDonorCode { get; set; }
        [Required]
        public DateTime LastUpdateFileUploadTime { get; set; }
    }
    
    public static class DonorLogModelBuilder
    {
        public static void SetUpDonorLogModel(this EntityTypeBuilder<DonorLog> donorModel)
        {
            donorModel.HasKey(d => d.ExternalDonorCode);
            donorModel.HasIndex(d => d.ExternalDonorCode).IsUnique();
        }
    }
}