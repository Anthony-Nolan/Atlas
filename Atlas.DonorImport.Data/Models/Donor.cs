using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Utils.Extensions;
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

        [MaxLength(256)]
        public string EthnicityCode { get; set; }

        [MaxLength(256)]
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

        /// <summary>
        /// Calculates a hash of donor data.
        /// Used to efficiently determine whether an inbound donor's details matches one already stored in the system. 
        /// </summary>
        public string CalculateHash()
        {
            return
                $"{DonorId}|{DonorType}|{EthnicityCode}|{RegistryCode}|{A_1}|{A_2}|{B_1}|{B_2}|{C_1}|{C_2}|{DPB1_1}|{DPB1_2}|{DQB1_1}|{DQB1_2}|{DRB1_1}|{DRB1_2}"
                    .ToMd5Hash();
        }
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