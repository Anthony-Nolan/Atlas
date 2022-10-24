using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Sql.BulkInsert;
using Atlas.DonorImport.Data.Context;

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
    }
}