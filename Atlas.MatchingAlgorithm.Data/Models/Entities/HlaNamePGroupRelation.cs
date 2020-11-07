using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Config;

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    public class HlaNamePGroupRelation
    {
        internal static string TableName(Locus locus) => $"HlaNamePGroupRelationAt{locus.ToString().ToUpperInvariant()}";

        public long Id { get; set; }

        [NotNull]
        public int HlaNameId { get; set; }

        [NotNull]
        public int PGroupId { get; set; }
    }

    [Table("HlaNamePGroupRelationAtA")]
    public class HlaNamePGroupRelationAtA : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtB")]
    public class HlaNamePGroupRelationAtB : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtC")]
    public class HlaNamePGroupRelationAtC : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtDQB1")]
    public class HlaNamePGroupRelationAtDqb1 : HlaNamePGroupRelation
    {
    }

    [Table("HlaNamePGroupRelationAtDRB1")]
    public class HlaNamePGroupRelationAtDrb1 : HlaNamePGroupRelation
    {
    }
}