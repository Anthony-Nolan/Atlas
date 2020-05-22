using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// ReSharper disable InconsistentNaming

namespace Atlas.DonorImport.Data.Models
{
    [Table("Donors")]
    public class Donor
    {
        public int Id { get; set; }

        [MaxLength(64)]
        public string DonorId { get; set; }

        public DatabaseDonorType DonorType { get; set; }
        public string EthnicityCode { get; set; }
        public string RegistryCode { get; set; }
        public string A_1 { get; set; }
        public string A_2 { get; set; }
        public string B_1 { get; set; }
        public string B_2 { get; set; }
        public string C_1 { get; set; }
        public string C_2 { get; set; }
        public string DPB1_1 { get; set; }
        public string DPB1_2 { get; set; }
        public string DQB1_1 { get; set; }
        public string DQB1_2 { get; set; }
        public string DRB1_1 { get; set; }
        public string DRB1_2 { get; set; }
        public string Hash { get; set; }
    }

    public static class DonorModelBuilder

    {
        public static void SetUpDonorModel(this EntityTypeBuilder<Donor> donorModel)

        {
            donorModel.Property(p => p.Id).ValueGeneratedOnAdd();

            donorModel.HasIndex(d => d.DonorId).IsUnique();

            donorModel.HasIndex(d => d.Hash);
        }
    }
}