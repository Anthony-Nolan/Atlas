using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.MatchPrediction.Data.Models
{
    public class HaplotypeFrequencySet
    {
        public int Id { get; set; }

        [MaxLength(256)]
        public string RegistryCode { get; set; }

        [MaxLength(256)]
        public string EthnicityCode { get; set; }

        public int PopulationId { get; set; }
        public string HlaNomenclatureVersion { get; set; }
        public bool Active { get; set; }
        public string Name { get; set; }
        public DateTimeOffset DateTimeAdded { get; set; }
    }

    internal static class HaplotypeFrequencySetModelBuilder
    {
        public static void SetUpModel(this EntityTypeBuilder<HaplotypeFrequencySet> modelBuilder)
        {
            modelBuilder.HasIndex(d => new { d.EthnicityCode, d.RegistryCode })
                .HasName("IX_RegistryCode_And_EthnicityCode")
                .IsUnique()
                .HasFilter("[Active] = 'True'");
        }
    }
}