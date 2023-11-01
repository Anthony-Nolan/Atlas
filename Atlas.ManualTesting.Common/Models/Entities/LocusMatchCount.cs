using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Sql.BulkInsert;

namespace Atlas.ManualTesting.Common.Models.Entities
{
    // ReSharper disable InconsistentNaming

    public class LocusMatchCount : IBulkInsertModel
    {
        public int Id { get; set; }
        public int MatchedDonor_Id { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public Locus Locus { get; set; }

        public int? MatchCount { get; set; }
    }
}