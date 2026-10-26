using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.MatchPrediction.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Data.Models
{
    [Table(TableName)]
    public class HaplotypeFrequencySet
    {
        internal const string TableName = "HaplotypeFrequencySets";
        internal static readonly string QualifiedTableName = $"{MatchPredictionContext.Schema}.{TableName}";
        public int Id { get; set; }

        [MaxLength(256)]
        public string RegistryCode { get; set; }

        [MaxLength(256)]
        public string EthnicityCode { get; set; }

        /// <summary>
        /// Source data for frequency sets is identified by a Population Id.
        /// Internally we use metadata (e.g. registry, ethnicity) for set selection, and an internal ID as the set primary key.
        /// Population ID is stored to enable quick cross-referencing to the source data, but is not used by ATLAS.
        /// </summary>
        public int PopulationId { get; set; }
        public string HlaNomenclatureVersion { get; set; }
        public bool Active { get; set; }
        public string Name { get; set; }
        public DateTimeOffset DateTimeAdded { get; set; }

        /// <summary>
        /// The SHA-256 of the frequencies of the set, in the order of their ids, without the set id: sets with the same
        /// hash have the same rows, and the caches keep one entry for all of them. The import writes it before it
        /// activates the set. Null for a set of an older import, until the backfill
        /// (<c>BackfillHaplotypeFrequencySetContentHashes</c>) writes it: such a set is cached by its own id.
        /// </summary>
        [Column(TypeName = "binary(32)")]
        public byte[] ContentHash { get; set; }
    }

    internal static class HaplotypeFrequencySetModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<HaplotypeFrequencySet> modelBuilder)
        {
            modelBuilder.HasIndex(d => new { d.EthnicityCode, d.RegistryCode })
                .HasName("IX_RegistryCode_And_EthnicityCode")
                .IsUnique()
                .HasFilter("[Active] = 'True'");
            modelBuilder
                .HasIndex(f => new { f.Active });
        }
    }
}