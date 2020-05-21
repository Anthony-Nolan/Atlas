using Atlas.DonorImport.Data.Models;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Models.Database
{
    [TestFixture]
    public class DonorTests
    {
        /// <summary>
        /// This test is a snapshot of the donor hash implementation, rather than a logical assertion.
        /// The calculated hash is stored in the donors table for efficient lookups - changing the underlying hash implementation would
        /// invalidate such calculated hashes, and the implications should be carefully considered. 
        /// </summary>
        [Test]
        public void DonorHash_ForDonorWithAllFieldPopulated_IsCalculatedAsExpected()
        {
            var donor = new Donor
            {
                DonorId = 1,
                DonorType = DatabaseDonorType.Adult,
                EthnicityCode = "ethnicity",
                RegistryCode = "registry",
                A_1 = "hla-a-1",
                A_2 = "hla-a-2",
                B_1 = "hla-b-1",
                B_2 = "hla-b-2",
                C_1 = "hla-c-1",
                C_2 = "hla-c-2",
                DPB1_1 = "hla-dpb1-1",
                DPB1_2 = "hla-dpb1-2",
                DQB1_1 = "hla-dqb1-1",
                DQB1_2 = "hla-dqb1-2",
                DRB1_1 = "hla-drb1-1",
                DRB1_2 = "hla-drb1-2",
            };

            donor.CalculateHash().Should().Be("qlL3cKZWsvUhFzUOEYXUdw==");
        }
        /// <summary>
        /// This test is a snapshot of the donor hash implementation, rather than a logical assertion.
        /// The calculated hash is stored in the donors table for efficient lookups - changing the underlying hash implementation would
        /// invalidate such calculated hashes, and the implications should be carefully considered. 
        /// </summary>
        [Test]
        public void DonorHash_ForMinimalDonor_IsCalculatedAsExpected()
        {
            var donor = new Donor
            {
                DonorId = 1,
                DonorType = DatabaseDonorType.Adult,
                A_1 = "hla-a-1",
                A_2 = "hla-a-2",
                B_1 = "hla-b-1",
                B_2 = "hla-b-2",
                DRB1_1 = "hla-drb1-1",
                DRB1_2 = "hla-drb1-2",
            };

            donor.CalculateHash().Should().Be("OD+1NI1gNnqOlB9CZWLrdg==");
        }
    }
}