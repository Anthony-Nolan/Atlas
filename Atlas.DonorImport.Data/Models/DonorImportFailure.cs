using Atlas.DonorImport.Data.Context;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Sql.BulkInsert;

namespace Atlas.DonorImport.Data.Models
{
    [Table(TableName)]
    public class DonorImportFailure : IBulkInsertModel
    {
        internal const string TableName = "DonorImportFailures";
        internal static readonly string QualifiedTableName = $"{DonorContext.Schema}.{TableName}";

        public int Id { get; set; }
        
        [MaxLength(64)]
        public string ExternalDonorCode { get; set; }

        [MaxLength(64)]
        public string DonorType { get; set; }

        [MaxLength(256)]
        public string EthnicityCode { get; set; }

        [MaxLength(256)]
        public string RegistryCode { get; set; }

        public string UpdateFile { get; set; }

        [MaxLength(256)]
        public string UpdateProperty { get; set; }

        public string FailureReason { get; set; }

        public DateTimeOffset FailureTime { get; set; }
    }

    internal static class DonorImportFailureModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<DonorImportFailure> donorImportFailureModel)
        {
            donorImportFailureModel.HasKey(m => m.Id);
            donorImportFailureModel.Property(m => m.Id).ValueGeneratedOnAdd();
            donorImportFailureModel.HasIndex(m => m.ExternalDonorCode);
            donorImportFailureModel.HasIndex(m => m.UpdateFile);
            donorImportFailureModel.HasIndex(m => new { m.DonorType, m.EthnicityCode, m.RegistryCode, m.FailureReason, m.FailureTime });
        }
    }
}
