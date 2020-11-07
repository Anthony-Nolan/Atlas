using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Config;

// ReSharper disable ClassNeverInstantiated.Global - Instantiated by EF
// ReSharper disable UnusedMember.Global - Used by EF

namespace Atlas.MatchingAlgorithm.Data.Models.Entities
{
    /// <summary>
    /// Relation between donor and expanded P-Groups for the donor's HLA.
    /// Model used only to manage database schema via EF - access is purely in SQL, and relations are deserialised to other types on access.
    /// </summary>
    public abstract class MatchingHla
    {
        internal static string TableName(Locus locus) => $"MatchingHlaAt{locus.ToString().ToUpperInvariant()}";

        public long Id { get; set; }
        public int TypePosition { get; set; }

        [NotNull]
        public int DonorId { get; set; }

        [NotNull]
        public int HlaNameId { get; set; }
    }

    [Table("MatchingHlaAtA")]
    public class MatchingHlaAtA : MatchingHla
    {
    }

    [Table("MatchingHlaAtB")]
    public class MatchingHlaAtB : MatchingHla
    {
    }

    [Table("MatchingHlaAtC")]
    public class MatchingHlaAtC : MatchingHla
    {
    }

    [Table("MatchingHlaAtDQB1")]
    public class MatchingHlaAtDqb1 : MatchingHla
    {
    }

    [Table("MatchingHlaAtDRB1")]
    public class MatchingHlaAtDrb1 : MatchingHla
    {
    }
}