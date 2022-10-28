using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Sql.BulkInsert;
using Atlas.DonorImport.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.DonorImport.Data.Models
{
    [Table(TableName)]
    public class PublishableDonorUpdate : IBulkInsertModel
    {
        public const string TableName = "PublishableDonorUpdates";
        internal const string QualifiedTableName = $"{DonorContext.Schema}.{TableName}";

        public int Id { get; set; }

        public int DonorId { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(MAX)")]
        public string SearchableDonorUpdate { get; set; }

        [BulkInsertIgnore]
        public DateTimeOffset CreatedOn { get; set; }

        [BulkInsertIgnore]
        public bool IsPublished { get; set; }

        [BulkInsertIgnore]
        public DateTimeOffset? PublishedOn { get; set; }
    }

    internal static class PublishableDonorUpdateModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<PublishableDonorUpdate> model)
        {
            model.Property(x => x.CreatedOn).HasDefaultValueSql("GETUTCDATE()");

            model
                .HasIndex(x => x.IsPublished)
                .HasFilter($"[{nameof(PublishableDonorUpdate.IsPublished)}] = 0");

            model
                .HasIndex(x => new { x.PublishedOn, x.IsPublished })
                .HasFilter($"[{nameof(PublishableDonorUpdate.IsPublished)}] = 1");
        }
    }
}