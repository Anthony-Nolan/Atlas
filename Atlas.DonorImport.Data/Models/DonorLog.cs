using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.DonorImport.Data.Context;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.DonorImport.Data.Models
{
    [Table(TableName)]
    public class DonorLog
    {
        internal const string TableName = "DonorLogs";
        internal static readonly string QualifiedTableName = $"{DonorContext.Schema}.{TableName}";
        
        [Required]
        public string ExternalDonorCode { get; set; }
        [Required]
        public DateTime LastUpdateFileUploadTime { get; set; }
    }
    
    public static class DonorLogModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<DonorLog> donorModel)
        {
            donorModel.HasKey(d => d.ExternalDonorCode);
            donorModel.HasIndex(d => d.ExternalDonorCode).IsUnique();
        }
    }
}